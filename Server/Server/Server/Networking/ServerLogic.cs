using LiteNetLib;
using LiteNetLib.Utils;
using Logic.Packets;
using Server.Utils;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Server.Core;
using Server.Players;

namespace Server.Networking
{
    public enum Session
    {
        Stop,
        Start
    }
    public class ServerLogic : INetEventListener
    {
        private NetManager netManager;
        private NetPacketProcessor packetProcessor;
        private int port = 10515;
        private ushort _serverTick;
        private ServerState _serverState;
        private LogicTimer _logicTimer;
        private NetDataWriter _cachedWriter = new NetDataWriter();
        private PlayerInputPacket _cachedCommand = new PlayerInputPacket();
        public PlayerState[] PlayerStates;
        public Session currentSession = Session.Stop;
        private RoomManager roomManager;

        public const int MaxPlayers = 64;
        public ushort Tick => _serverTick;

        public void Run()
        {
            _logicTimer = new LogicTimer(OnLogicUpdate);
            roomManager = new RoomManager(this);
            packetProcessor = new NetPacketProcessor();
            packetProcessor.RegisterNestedType((w, v) => w.PutV2(v), reader => reader.GetVector2());
            packetProcessor.RegisterNestedType((w, v) => w.PutV3(v), r => r.GetVector3());
            packetProcessor.RegisterNestedType((w, v) => w.PutQuat(v), r => r.GetQuaternion());
            packetProcessor.RegisterNestedType((w, v) => w.Put(v), r => r.GetString());
            PlayerStates = new PlayerState[MaxPlayers];
            packetProcessor.RegisterNestedType<PlayerState>();
            packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
            packetProcessor.SubscribeReusable<PlayerReadyPacket, NetPeer>(OnPlayerReady);
            packetProcessor.SubscribeReusable<PlayerResetPacket>(OnPlayerReset);

            netManager = new NetManager(this)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };

            netManager.NatPunchEnabled = true;
            netManager.UnconnectedMessagesEnabled = true;
            netManager.UpdateTime = 15;
            netManager.DisconnectTimeout = 5000;
            netManager.SimulatePacketLoss = false;
            netManager.SimulateLatency = false;
            netManager.BroadcastReceiveEnabled = true;

            if (netManager.IsRunning)
                return;

            if (!netManager.Start(port))
            {
                Console.WriteLine("Failed to start server.");
                return;
            }

            Console.WriteLine("start server ++++.");

            var lanManager = new LanManager(serverPort: 1025, clientPort: 1024, debug: true);
            lanManager.StartInBackground();

            _logicTimer.Start();

            while (!Console.KeyAvailable)
            {
                netManager.PollEvents();
                _logicTimer.Update();
                SessionLoop();
                System.Threading.Thread.Sleep(15);
            }
            netManager.Stop();
            lanManager.Stop();
        }

        private void SessionLoop()
        {
            if (currentSession == Session.Start) return;
            if (roomManager._onlinePlayers.Count == 0) return;

            int count = 0;
            foreach (var player in roomManager._onlinePlayers)
            {
                if (player.isReady == true) count++;
            }
            if (count == roomManager._onlinePlayers.Count)
            {
                currentSession = Session.Start;
                netManager.SendToAll(WritePacket(new SessionStartPacket { }), DeliveryMethod.ReliableOrdered);
                roomManager.StartBots();
            }
        }

        private void OnLogicUpdate()
        {
            _serverTick = (ushort)((_serverTick + 1) % NetworkGeneral.MaxGameSequence);

            for (int i = 0; i < roomManager._onlinePlayers.Count; i++)
            {
                var p = roomManager._onlinePlayers[i];
                p.Update(LogicTimer.FixedDelta);
                PlayerStates[i] = p.NetworkState;
            }
            if (_serverTick % 2 == 0)
            {
                _serverState.Tick = _serverTick;
                _serverState.PlayerStates = PlayerStates;
                int pCount = roomManager._onlinePlayers.Count;

                foreach (OnlinePlayer p in roomManager._onlinePlayers)
                {
                    int statesMax = p.AssociatedPeer.GetMaxSinglePacketSize(DeliveryMethod.Sequenced) - ServerState.HeaderSize;
                    statesMax /= PlayerState.Size;

                    for (int s = 0; s < (pCount - 1) / statesMax + 1; s++)
                    {
                        //TODO: divide
                        _serverState.LastProcessedCommand = p.LastProcessedCommandId;
                        _serverState.PlayerStatesCount = pCount;
                        _serverState.StartState = s * statesMax;
                        p.AssociatedPeer.Send(WriteSerializable(PacketType.ServerState, _serverState), DeliveryMethod.Sequenced);
                    }
                }
            }

            for (int i = 0; i < roomManager._bots.Count; i++)
            {
                var p = roomManager._bots[i];
                p.Update(LogicTimer.FixedDelta);
            }

            for (int i = 0; i < roomManager._projectiles.Count; i++)
            {
                if (roomManager._projectiles[i].isAlive == true)
                {
                    roomManager._projectiles[i].Update(LogicTimer.FixedDelta);
                    foreach (var bot in roomManager._bots)
                    {
                        if (roomManager._projectiles[i].isAlive)
                        {
                            if (bot.Hit(roomManager._projectiles[i]) == true)
                            {
                                var healthPacket = new HealthUpdatePacket
                                {
                                    PlayerId = bot.id,
                                    Health = bot._health,
                                    isBot = true
                                };

                                netManager.SendToAll(WritePacket(healthPacket), DeliveryMethod.ReliableOrdered);
                                roomManager._projectiles[i].isAlive = false;
                            }
                        }
                    }

                    foreach (OnlinePlayer p in roomManager._onlinePlayers)
                    {
                        if (p.Hit(roomManager._projectiles[i]) == true)
                        {
                            var healthPacket = new HealthUpdatePacket
                            {
                                PlayerId = p.id,
                                Health = p._health,
                                isBot = false
                            };

                            netManager.SendToAll(WritePacket(healthPacket), DeliveryMethod.ReliableOrdered);
                            roomManager._projectiles[i].isAlive = false;
                        }
                        roomManager._projectiles[i].projectileMovementPackets.Position = roomManager._projectiles[i]._position;
                        roomManager._projectiles[i].projectileMovementPackets.OwnerId = roomManager._projectiles[i]._ownerId;
                        roomManager._projectiles[i].projectileMovementPackets.ProjectileId = roomManager._projectiles[i].id;
                        roomManager._projectiles[i].projectileMovementPackets.isBot = roomManager._projectiles[i].isBot;
                        p.AssociatedPeer.Send(WriteSerializable(PacketType.ProjectileMovement, roomManager._projectiles[i].projectileMovementPackets), DeliveryMethod.Unreliable);
                    }
                }
                else
                {
                    roomManager._projectilesToRemove.Add(roomManager._projectiles[i]);
                }
            }

            if (roomManager._projectilesToRemove.Count > 0)
            {
                foreach (var ptr in roomManager._projectilesToRemove)
                {
                    roomManager._projectiles.Remove(ptr);
                    netManager.SendToAll(WritePacket(new ProjectileDestroyPacket { Id = ptr.id, OwnerId = ptr._ownerId, isBot = ptr.isBot }), DeliveryMethod.ReliableOrdered);
                }
                roomManager._projectilesToRemove.Clear();
            }
            roomManager.BotsUpdate(LogicTimer.FixedDelta);
        }

        private void OnJoinReceived(JoinPacket joinPacket, NetPeer peer)
        {
            Console.WriteLine("[S] Join packet received: " + joinPacket.UserName);

            //Randomize the start position
            //Vector3 pos = new Vector3(RandomHelper.Range(-5f, 5f), 1.0f, RandomHelper.Range(-5f, 5f));
            Vector3 pos = roomManager.GetSafeSpawnPosition();
            //Create New player
            var newPlayer = new OnlinePlayer(joinPacket.UserName, peer, pos);
            roomManager._onlinePlayers.Add(newPlayer);

            //Send join accept
            var ja = new JoinAcceptPacket { Id = newPlayer.id, Position = pos, ServerTick = _serverTick };
            peer.Send(WritePacket(ja), DeliveryMethod.ReliableOrdered);

            //Send to old players info about new player
            var pj = new PlayerJoinedPacket
            {
                Id = newPlayer.id,
                Position = pos,
                UserName = joinPacket.UserName,
                NewPlayer = true,
                InitialPlayerState = newPlayer.NetworkState,
                Health = 100,
                ServerTick = _serverTick
            };

            netManager.SendToAll(WritePacket(pj), DeliveryMethod.ReliableOrdered, peer);

            //Send to new player info about old players
            pj.NewPlayer = false;
            foreach (var player in roomManager._onlinePlayers)
            {
                if (player.AssociatedPeer != peer)
                {
                    pj.Id = player.id;
                    pj.Position = player._position;
                    pj.UserName = player.name;
                    pj.InitialPlayerState = player.NetworkState;
                    pj.Health = player._health;
                    peer.Send(WritePacket(pj), DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private void OnPlayerReady(PlayerReadyPacket playerReadyPacket, NetPeer peer)
        {
            foreach (var player in roomManager._onlinePlayers)
            {
                if (player.id == playerReadyPacket.Id)
                {
                    player.isReady = true;
                    break;
                }
            }
        }


        private void OnPlayerReset(PlayerResetPacket playerResetPacket)
        {
            foreach (var player in roomManager._onlinePlayers)
            {
                if (player.id == playerResetPacket.Id)
                {
                    player._health = 100;
                    player.isDead = false;
                    player._position = roomManager.GetSafeSpawnPosition();
                    break;
                }
            }
            //echo for all to receive
            netManager.SendToAll(WritePacket(playerResetPacket), DeliveryMethod.ReliableOrdered);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("[S] Player connected: " + peer.Address);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("[S] Player disconnected: " + disconnectInfo.Reason);

            if (peer.Tag != null)
            {
                int playerId = peer.Id;
                bool contains = false;
                OnlinePlayer playerFound = null;
                foreach (var player in roomManager._onlinePlayers)
                {
                    if (player.id == playerId)
                    {
                        contains = true;
                        playerFound = player;
                        break;
                    }
                }
                if (playerFound != null)
                {
                    roomManager._onlinePlayers.Remove(playerFound);

                }
                if (contains)
                {
                    var plp = new PlayerLeavedPacket { Id = (byte)peer.Id };
                    netManager.SendToAll(WritePacket(plp), DeliveryMethod.ReliableOrdered);
                }
                if (currentSession == Session.Start)
                {
                    if (roomManager._onlinePlayers.Count == 0)
                    {
                        currentSession = Session.Stop;
                        roomManager._bots.Clear();
                        roomManager._projectiles.Clear();
                        roomManager._projectilesToRemove.Clear();
                    }
                }
            }
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            Console.WriteLine("REQUEST TO JOIN");
            request.AcceptIfKey("ExampleGame");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine("[S] NetworkError: " + socketError);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;
            PacketType pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.Movement:
                    {
                        OnInputReceived(reader, peer);
                        break;
                    }
                case PacketType.Serialized:
                    {
                        packetProcessor.ReadAllPackets(reader, peer);
                        break;
                    }
                case PacketType.Shoot:
                    {
                        ShootPacket shoot = new ShootPacket();
                        shoot.Deserialize(reader);
                        HandleShoot(peer, shoot);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Unhandled packet: " + pt);
                        break;
                    }
            }
        }

        private void HandleShoot(NetPeer peer, ShootPacket packet)
        {
            Vector3 origin = packet.Origin;
            Vector3 dir = Vector3.Normalize(packet.Direction);
            byte newProjectileId = roomManager.GetNextProjectileId();

            Projectile projectile = new Projectile(peer, newProjectileId, origin, dir, (byte)peer.Id, false);
            roomManager._projectiles.Add(projectile);
            ShootProjectile(projectile.projectileSpawnPacket);
        }

        public void ShootProjectile(ProjectileSpawnPacket packet)
        {
            foreach (var player in roomManager._onlinePlayers)
            {
                player.AssociatedPeer.Send(WriteSerializable(PacketType.ProjectileSpawn, packet), DeliveryMethod.ReliableOrdered);
            }
        }

        private void OnInputReceived(NetPacketReader reader, NetPeer peer)
        {
            if (peer.Tag == null)
                return;

            _cachedCommand.Deserialize(reader);
            var player = (OnlinePlayer)peer.Tag;
            bool antilagApplied = roomManager.EnableAntilag(player);
            player.ApplyInput(_cachedCommand, LogicTimer.FixedDelta);
            if (antilagApplied)
                roomManager.DisableAntilag();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            if (peer.Tag != null)
            {
                var p = (OnlinePlayer)peer.Tag;
                p.Ping = latency;
            }
        }

        public void BroadcastBotPosition(BotPlayer bot)
        {
            var botMovementPacket = new BotMovementPacket
            {
                botId = bot.id,
                Position = bot._position,
                Rotation = bot._rotation,
            };
            foreach (var player in roomManager._onlinePlayers)
            {
                player.AssociatedPeer.Send(WriteSerializable(PacketType.BotMovement, botMovementPacket), DeliveryMethod.Unreliable);
            }
        }

        public void SendBotSpawn(BotSpawnPacket packet)
        {
            netManager.SendToAll(WritePacket(packet), DeliveryMethod.ReliableOrdered);
        }

        public NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte)type);
            packet.Serialize(_cachedWriter);
            return _cachedWriter;
        }

        public NetDataWriter WritePacket<T>(T packet) where T : class, new()
        {
            _cachedWriter.Reset();
            _cachedWriter.Put((byte)PacketType.Serialized);
            packetProcessor.Write(_cachedWriter, packet);
            return _cachedWriter;
        }
    }
}
