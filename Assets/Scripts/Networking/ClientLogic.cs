using Adapters;
using Adapters.Character;
using Core.Player;
using Fortis.UI;
using Fortis.Utils;
using LiteNetLib;
using LiteNetLib.Utils;
using Logic.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEditor.Sprites;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.UIElements;
namespace Fortis.LAN
{
    public enum Session
    {
        Stop,
        Start
    }

    public class ClientLogic : Singleton<ClientLogic>, INetEventListener
    {
        public RoomManager roomManager;
        [SerializeField] private Text _debugText;
        public GameManager gameManager;
        private LanManager lanManager;
        public string serverAddress = "";

        private Action<DisconnectInfo> _onDisconnected;
        private NetManager _netManager;
        private NetDataWriter _writer;
        private NetPacketProcessor _packetProcessor;

        private string _userName;
        private ServerState _cachedServerState;
        private ProjectileSpawnPacket _cachedProjectileSpawnPacket;
        private ushort _lastServerTick;
        private NetPeer _server;
        private float _ping;

        public static LogicTimer LogicTimer { get; private set; }
        // Start is called before the first frame update

        public IEnumerator Start()
        {
            serverAddress = "";
            lanManager = new LanManager(serverPort: 1025, clientPort: 1024, debug: false);
            lanManager.ScanHost();
            lanManager.StartClient();

            while (!lanManager.socketIsCreated)
            {
                yield return new WaitForEndOfFrame();
            }
            IEnumerator enumerator;
            enumerator = lanManager.SendPing();
            StartCoroutine(enumerator);

            while (lanManager.address.Equals(""))
            {
                yield return new WaitForEndOfFrame();
            }
            serverAddress = lanManager.address;

            UIController.instance.EnableConnectionPanel();
            UIController.instance.readyButton.onClick.AddListener(() =>
            {
                SendPlayerReady();
            });

            lanManager.CloseClient();

            _cachedServerState = new ServerState();
            _cachedProjectileSpawnPacket = new ProjectileSpawnPacket();
            System.Random r = new System.Random();

            _userName = Environment.MachineName + " " + r.Next(100000);
            LogicTimer = new LogicTimer(OnLogicUpdate);
            _writer = new NetDataWriter();
            roomManager = new RoomManager(this);

            _packetProcessor = new NetPacketProcessor();
            _packetProcessor.RegisterNestedType((w, v) => w.PutV2(v), reader => reader.GetVector2());
            _packetProcessor.RegisterNestedType((w, v) => w.PutV3(v), reader => reader.GetVector3());
            _packetProcessor.RegisterNestedType((w, v) => w.PutQuat(v), r => r.GetQuaternion());
            _packetProcessor.RegisterNestedType((w, v) => w.Put(v), r => r.GetString()); // if JoinPacket has string fields

            _packetProcessor.RegisterNestedType<PlayerState>();
            _packetProcessor.RegisterNestedType<ProjectileSpawnPacket>();
            _packetProcessor.RegisterNestedType<ProjectileMovementPacket>();
            _packetProcessor.RegisterNestedType<BotMovementPacket>();
            _packetProcessor.SubscribeReusable<PlayerJoinedPacket>(OnPlayerJoined);
            _packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);
            _packetProcessor.SubscribeReusable<PlayerLeavedPacket>(OnPlayerLeaved);
            _packetProcessor.SubscribeReusable<HealthUpdatePacket>(OnHealthUpdate);
            _packetProcessor.SubscribeReusable<BotSpawnPacket>(OnBotSpawn);
            _packetProcessor.SubscribeReusable<ProjectileDestroyPacket>(OnProjectileDestroy);
            _packetProcessor.SubscribeReusable<SessionStartPacket>(OnSessionStart);
            _packetProcessor.SubscribeReusable<PlayerResetPacket>(OnPlayerReset);

            _netManager = new NetManager(this)
            {
                AutoRecycle = true,
                IPv6Enabled = false
            };

            _netManager.NatPunchEnabled = true;
            _netManager.UnconnectedMessagesEnabled = true;
            _netManager.UpdateTime = 15;
            _netManager.DisconnectTimeout = 5000;
            _netManager.SimulatePacketLoss = false;
            _netManager.SimulateLatency = false;
            _netManager.BroadcastReceiveEnabled = true;
            _netManager.Start();

            _onDisconnected += OnDisconnected;
            _netManager.Connect(serverAddress, 10515, "ExampleGame");
        }

        private void OnDisconnected(DisconnectInfo info)
        {
            //_uiObject.SetActive(true);
            //_disconnectInfoField.text = info.Reason.ToString();
        }
        private void OnApplicationQuit()
        {
            if (lanManager != null)
            {
                lanManager.CloseClient();
            }
        }

        private void OnDestroy()
        {
            if (_netManager != null)
                _netManager.Stop();
        }

        void Update()
        {
            if (_netManager == null) return;
            if (_netManager.IsRunning == false) return;
            _netManager.PollEvents();
            LogicTimer.Update();
            if (_debugText != null)
            {
                if (roomManager.clientPlayer != null)
                    _debugText.text =
                        string.Format(
                            $"LastServerTick: {_lastServerTick}\n" +
                            $"StoredCommands: {roomManager.clientPlayer.StoredCommands}\n" +
                            $"Ping: {_ping}");
                else
                    _debugText.text = "Disconnected";
            }
        }

        private void OnLogicUpdate()
        {
            foreach (var kv in roomManager._players)
            {
                kv.Value.Player.Update(LogicTimer.FixedDelta);
            }
        }

        private void OnPlayerJoined(PlayerJoinedPacket packet)
        {
            Debug.Log($"[C] Player joined: {packet.UserName} " + packet.Id);
            if (roomManager._players.ContainsKey(packet.Id))
                return;
            var remotePlayer = new RemotePlayer(packet, packet.Id, packet.UserName);
            PlayerView view = gameManager.SpawnPlayer(packet.Id, packet.Position);
            view.transform.position = packet.Position;
            remotePlayer.Position = packet.Position;
            remotePlayer._health = packet.Health;
            ((RemotePlayerView)view).Setup(remotePlayer);
            roomManager._players.Add(remotePlayer.Id, new PlayerHandler(remotePlayer, view));
        }

        private void OnPlayerLeaved(PlayerLeavedPacket packet)
        {
            Debug.Log($"[C] Player leaved: {packet.Id}");
            if (roomManager._players.TryGetValue(packet.Id, out var handler))
            {
                roomManager._players.Remove(packet.Id);
                Destroy(handler.View.gameObject);
            }
        }

        private void OnJoinAccept(JoinAcceptPacket packet)
        {
            Debug.Log("[C] Join accept. Received player id: " + packet.Id);
            _lastServerTick = packet.ServerTick;
            var clientPlayer = new ClientPlayer(this, packet.Id, gameManager._inputListener, _userName);
            clientPlayer.Position = packet.Position;
            ClientPlayerView view = (ClientPlayerView)gameManager.SpawnPlayer(packet.Id, packet.Position, true);
            clientPlayer.transform = view.transform;
            view.Setup(clientPlayer);

            roomManager.clientPlayer = clientPlayer;
            roomManager._players.Add(roomManager.clientPlayer.Id, new PlayerHandler(roomManager.clientPlayer, view));
        }

        private void OnServerState()
        {
            //skip duplicate or old because we received that packet unreliably
            if (NetworkGeneral.SeqDiff(_cachedServerState.Tick, _lastServerTick) <= 0)
                return;
            _lastServerTick = _cachedServerState.Tick;
            ApplyServerState(ref _cachedServerState);
        }

        private void OnSessionStart(SessionStartPacket packet)
        {
            roomManager.currentSession = Session.Start;
            if (roomManager.clientPlayer.isDead)
            {
                if (roomManager._players.TryGetValue(roomManager.clientPlayer.Id, out var handler))
                {
                    SendPlayerReset();
                }
            }
            UIController.instance.DisableErrorAndConnectionPanel();
        }

        private void OnPlayerReset(PlayerResetPacket playerResetPacket)
        {
            if (!roomManager._players.TryGetValue(playerResetPacket.Id, out var handler))
                return;
            handler.Player._health = 100;
            handler.Player.isDead = false;
            handler.View.SetMaterialToOpaque();
        }

        private void OnHealthUpdate(HealthUpdatePacket packet)
        {
            if (packet.isBot == false)
            {
                if (!roomManager._players.TryGetValue(packet.PlayerId, out var handler))
                    return;
                handler.Player._health = packet.Health;
                if (handler.Player._health <= 0)
                {
                    handler.Player.isDead = true;
                    handler.View.SetMaterialToTransparent();
                    if (handler.Player.Id == roomManager.clientPlayer.Id)
                    {
                        UIController.instance.EnableError("You Died");
                        UIController.instance.EnableConnectionResetPanel();
                        UIController.instance.readyButton.onClick.AddListener(() =>
                        {
                            SendPlayerReset();
                        });
                    }
                }
            }
            else
            {
                if (!roomManager.bots.TryGetValue(packet.PlayerId, out var handler))
                    return;
                handler.Player._health = packet.Health;
                if (handler.Player._health <= 0)
                {
                    handler.Player.isDead = true;
                    handler.View.SetMaterialToTransparent();
                }
            }
        }

        private void OnProjectileSpawned()
        {
            if (_cachedProjectileSpawnPacket.isBot == false)
            {
                if (!roomManager._players.TryGetValue(_cachedProjectileSpawnPacket.OwnerId, out var handler))
                    return;
                var sp = new ServerProjectile(_cachedProjectileSpawnPacket.Position, _cachedProjectileSpawnPacket.Direction);
                ServerProjectileView spw = (ServerProjectileView)(gameManager.HandleShoot(_cachedProjectileSpawnPacket.Position, _cachedProjectileSpawnPacket.Direction));

                sp.id = _cachedProjectileSpawnPacket.ProjectileId;
                sp.ownerId = _cachedProjectileSpawnPacket.OwnerId;
                spw.Setup(sp);
                handler.Player.projectiles.Add(sp);
            }
            else
            {
                if (!roomManager.bots.TryGetValue(_cachedProjectileSpawnPacket.OwnerId, out var handler))
                    return;
                var sp = new ServerProjectile(_cachedProjectileSpawnPacket.Position, _cachedProjectileSpawnPacket.Direction);
                ServerProjectileView spw = (ServerProjectileView)(gameManager.HandleShoot(_cachedProjectileSpawnPacket.Position, _cachedProjectileSpawnPacket.Direction));

                sp.id = _cachedProjectileSpawnPacket.ProjectileId;
                sp.ownerId = _cachedProjectileSpawnPacket.OwnerId;
                spw.Setup(sp);
                handler.Player.projectiles.Add(sp);
            }
        }

        public void OnBotSpawn(BotSpawnPacket packet)
        {
            if (roomManager.bots.TryGetValue(packet.Id, out var handler))
            {
                handler.Player._health = 100;
                handler.Player.isDead = false;
                handler.Player.Position = packet.Position;
                handler.Player.transform.position = packet.Position;
                handler.View.SetMaterialToOpaque();
            }
            else
            {
                PlayerView botPlayerView1 = gameManager.SpawnPlayer(packet.Id, packet.Position, false, true);
                botPlayerView1.transform.position = packet.Position;
                var bot1 = new BotPlayer(packet.Id);
                bot1.Position = packet.Position;
                ((BotPlayerView)botPlayerView1).Setup(bot1);
                roomManager.bots.Add(packet.Id, new PlayerHandler(bot1, botPlayerView1));
                bot1.transform = botPlayerView1.transform;
            }
        }

        public void ApplyServerState(ref ServerState serverState)
        {
            for (int i = 0; i < serverState.PlayerStatesCount; i++)
            {
                var state = serverState.PlayerStates[i];
                if (!roomManager._players.TryGetValue(state.Id, out var handler))
                    continue;

                if (handler.Player.Id == roomManager.clientPlayer.Id)
                {
                    roomManager.clientPlayer.ReceiveServerState(serverState, state);
                }
                else
                {
                    var rp = (RemotePlayer)handler.Player;
                    rp.OnPlayerState(state);
                }
            }
        }

        private void OnProjectileMovement(ProjectileMovementPacket packet)
        {
            if (packet.isBot == false)
            {
                if (!roomManager._players.TryGetValue(packet.OwnerId, out var handler))
                    return;
                foreach (var prj in handler.Player.projectiles)
                {
                    if (prj.id == packet.ProjectileId)
                    {
                        prj.Position = packet.Position;
                        break;
                    }
                }
            }
            if (packet.isBot == true)
            {
                if (!roomManager.bots.TryGetValue(packet.OwnerId, out var handler))
                    return;
                foreach (var prj in handler.Player.projectiles)
                {
                    if (prj.id == packet.ProjectileId)
                    {
                        prj.Position = packet.Position;
                        break;
                    }
                }
            }
        }

        private void OnBotMovement(BotMovementPacket packet)
        {
            if (!roomManager.bots.TryGetValue(packet.botId, out var handler))
                return;
            roomManager.bots[packet.botId].Player.Position = packet.Position;
            roomManager.bots[packet.botId].Player.Rotation = packet.Rotation;
        }

        private void OnProjectileDestroy(ProjectileDestroyPacket packet)
        {
            if (packet.isBot == false)
            {
                if (!roomManager._players.TryGetValue(packet.OwnerId, out var handler))
                    return;
                ServerProjectile sp = null;
                foreach (var prj in handler.Player.projectiles)
                {
                    if (prj.id == packet.Id)
                    {
                        sp = (ServerProjectile)prj;
                        sp.Tick();
                        break;
                    }
                }
                handler.Player.projectiles.Remove(sp);
            }
            else
            {
                if (!roomManager.bots.TryGetValue(packet.OwnerId, out var handler))
                    return;
                ServerProjectile sp = null;
                foreach (var prj in handler.Player.projectiles)
                {
                    if (prj.id == packet.Id)
                    {
                        sp = (ServerProjectile)prj;
                        sp.Tick();
                        break;
                    }
                }
                handler.Player.projectiles.Remove(sp);
            }
        }

        public void TryShoot(ClientPlayer clientPlayer)
        {
            if (roomManager.currentSession == Session.Stop) return;
            if (roomManager.clientPlayer.isDead) return;

            var shootPacket = new ShootPacket
            {
                Origin = roomManager.clientPlayer.transform.position,
                Direction = roomManager.clientPlayer.transform.forward,
                FromPlayerId = roomManager.clientPlayer.Id
            };

            SendPacketSerializable(PacketType.Shoot, shootPacket, DeliveryMethod.Unreliable);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[C] Connected to server: ");
            _server = peer;

            SendPacket(new JoinPacket { UserName = _userName }, DeliveryMethod.ReliableOrdered);
            LogicTimer.Start();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (peer == _server)
            {
                HandleServerDisconnect();
            }
            if (!roomManager._players.TryGetValue(peer.Id, out var handler))
                return;
            roomManager._players.Clear();
            roomManager.clientPlayer = null;
            _server = null;
            LogicTimer.Stop();
            Debug.Log("[C] Disconnected from server: " + disconnectInfo.Reason);
            if (_onDisconnected != null)
            {
                _onDisconnected(disconnectInfo);
                _onDisconnected = null;
            }
        }

        private void HandleServerDisconnect()
        {
            foreach (var player in roomManager._players)
            {
                Destroy(player.Value.View.gameObject);
                foreach (var prj in player.Value.Player.projectiles)
                {
                    ServerProjectile sp = (ServerProjectile)prj;
                    sp.Tick();
                }
            }
            foreach (var bot in roomManager.bots)
            {
                Destroy(bot.Value.View.gameObject);
                foreach (var prj in bot.Value.Player.projectiles)
                {
                    ServerProjectile sp = (ServerProjectile)prj;
                    sp.Tick();
                }
            }
            roomManager._players.Clear();
            roomManager.clientPlayer = null;
            _server = null;
            roomManager.bots.Clear();
            StartCoroutine(Start());
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.Log("[C] NetworkError: " + socketError);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            byte packetType = reader.GetByte();
            if (packetType >= NetworkGeneral.PacketTypesCount)
                return;
            PacketType pt = (PacketType)packetType;
            switch (pt)
            {
                case PacketType.Spawn:
                    break;
                case PacketType.ServerState:
                    _cachedServerState.Deserialize(reader);
                    OnServerState();
                    break;
                case PacketType.Serialized:
                    _packetProcessor.ReadAllPackets(reader);
                    break;
                case PacketType.Shoot:
                    break;
                case PacketType.ProjectileSpawn:
                    _cachedProjectileSpawnPacket.Deserialize(reader);
                    OnProjectileSpawned();
                    break;
                case PacketType.ProjectileMovement:
                    ProjectileMovementPacket projectileMovementPacket = new ProjectileMovementPacket();
                    projectileMovementPacket.Deserialize(reader);
                    OnProjectileMovement(projectileMovementPacket);
                    break;
                case PacketType.BotMovement:
                    BotMovementPacket botMovementPacket = new BotMovementPacket();
                    botMovementPacket.Deserialize(reader);
                    OnBotMovement(botMovementPacket);
                    break;
                default:
                    Debug.Log("Unhandled packet: " + pt);
                    break;
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            _ping = latency;
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }

        public void SendPlayerReady()
        {
            SendPacket(new PlayerReadyPacket { Id = roomManager.clientPlayer.Id, IsReady = true }, DeliveryMethod.ReliableOrdered);
        }

        public void SendPlayerReset()
        {
            SendPacket(new PlayerResetPacket { Id = roomManager.clientPlayer.Id }, DeliveryMethod.ReliableOrdered);
        }

        public void SendPacketSerializable<T>(PacketType type, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)type);
            packet.Serialize(_writer);
            _server.Send(_writer, deliveryMethod);
        }

        public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
        {
            if (_server == null)
                return;
            _writer.Reset();
            _writer.Put((byte)PacketType.Serialized);
            _packetProcessor.Write(_writer, packet);
            _server.Send(_writer, deliveryMethod);
        }
    }
}