using Logic.Packets;
using Server.Networking;
using Server.Utils;
using Server.Players;
using System.Numerics;
using Server.Core;
namespace Server
{
    public class RoomManager
    {
        public List<Projectile> _projectilesToRemove;
        public List<OnlinePlayer> _onlinePlayers;
        public List<Projectile> _projectiles;
        public List<BotPlayer> _bots;

        private const float BOT_SPEED = 1f;
        private const float BOT_SHOOT_RANGE = 15f;
        private const float BOT_SHOOT_COOLDOWN_TIME = 2.5f;
        private const float PLAYER_MIN_DISTANCE = 2f;
        private const float BOT_AVOID_RADIUS = 1.0f;
        private const float BOT_REPULSION_STRENGTH = 3.0f;
        private const int BOT_LOW_HEALTH_THRESHOLD = 30;
        private const float BOT_RETREAT_SPEED_MULTIPLIER = 1.2f;

        const int maxBots = 3;
        private byte _nextProjectileId = 1;

        public byte GetNextProjectileId()
        {
            return _nextProjectileId++;
        }

        private byte _nextBotId = 1;

        public byte GetNextBotId()
        {
            return _nextBotId++;
        }

        private readonly ServerLogic serverLogic;
        private readonly AntilagSystem _antilagSystem;

        public RoomManager(ServerLogic serverLogic)
        {
            _projectilesToRemove = new List<Projectile>();
            _onlinePlayers = new List<OnlinePlayer>();
            _projectiles = new List<Projectile>();
            _bots = new List<BotPlayer>();

            this.serverLogic = serverLogic;
            _antilagSystem = new AntilagSystem(60, ServerLogic.MaxPlayers);

        }

        #region Antilag
        public bool EnableAntilag(OnlinePlayer forPlayer)
        {
            return _antilagSystem.TryApplyAntilag(_onlinePlayers, serverLogic.Tick, forPlayer.AssociatedPeer.Id);
        }

        public void DisableAntilag()
        {
            _antilagSystem.RevertAntilag(_onlinePlayers);
        }
        #endregion

        #region Players

        public Vector3 GetSafeSpawnPosition(int maxAttempts = 50)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 candidate = new Vector3(
                    RandomHelper.Range(-5f, 5f),
                    1,
                    RandomHelper.Range(-5f, 5f)
                );

                bool intersects = false;
                foreach (var player in _onlinePlayers)
                {
                    if (Collisions.CheckIntersection(
                        candidate.X, candidate.Y, candidate.Z,
                        candidate.X, candidate.Y, candidate.Z,
                        player
                    ))
                    {
                        intersects = true;
                        break;
                    }
                }

                if (!intersects)
                    return candidate;
            }

            //Fallback
            Console.WriteLine("Warning: Failed to find non-colliding spawn. Using fallback.");
            return new Vector3(0, 1, 0);
        }
        #endregion

        #region Projectiles

        #endregion

        #region Bots
        public OnlinePlayer GetClosestPlayer(Vector3 botPos)
        {
            OnlinePlayer closest = null;
            float minDist = float.MaxValue;

            foreach (var player in _onlinePlayers)
            {
                if (player.isDead) continue;
                float dist = Vector3.Distance(player._position, botPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = player;
                }
            }

            return closest;
        }

        public void StartBots()
        {
            //_bots.Add(new BotPlayer(201, new Vector3(10, 1, 10)));
            //_bots.Add(new BotPlayer(202, new Vector3(-10, 1, -10)));

            for (int i = 0; i < maxBots; i++)
            {
                byte botId = GetNextBotId();

                Vector3 spawnPosition = new Vector3(
                    -10 + (10 * i),
                    1f,
                    -10 + (10 * i)
                );

                _bots.Add(new BotPlayer(botId, spawnPosition, this));
                var botSpawn = new BotSpawnPacket
                {
                    Id = botId,
                    Position = spawnPosition
                };
                serverLogic.SendBotSpawn(botSpawn);
            }
        }

        public void Respawn(BotPlayer bot)
        {
            bot._health = 100;
            Vector3 pos = new Vector3(
                RandomHelper.Range(-10f, 10f),
                1,
                RandomHelper.Range(-10f, 10f)
            );

            bot._position = pos;
            bot.isDead = false;
            var botSpawn = new BotSpawnPacket
            {
                Id = bot.id,
                Position = pos
            };
            serverLogic.SendBotSpawn(botSpawn);
        }

        public void BotsUpdate(float delta)
        {
            if (_onlinePlayers.Count == 0) return;

            foreach (var bot in _bots)
            {
                if(bot.isDead) continue;

                bot.ShootCooldown -= delta;

                var closest = GetClosestPlayer(bot._position);
                if (closest == null)
                    continue;

                Vector3 toTarget = closest._position - bot._position;
                float distanceToPlayer = toTarget.Length();

                float targetYaw = MathF.Atan2(toTarget.X, toTarget.Z) * (180f / MathF.PI);
                bot._rotation = Extensions.LerpAngle(bot._rotation, targetYaw, delta * 5f);

                Vector3 moveDir = Vector3.Zero;

                //if (distanceToPlayer > PLAYER_MIN_DISTANCE)
                //{
                //    moveDir = Vector3.Normalize(toTarget);
                //}

                bool isRetreating = bot._health < BOT_LOW_HEALTH_THRESHOLD;

                if (isRetreating)
                {
                    if (distanceToPlayer < PLAYER_MIN_DISTANCE * 6f)
                    {
                        // Run away from player
                        moveDir = Vector3.Normalize(bot._position - closest._position);
                    }
                    // Skip shooting when retreating
                }
                else
                {
                    if (distanceToPlayer > PLAYER_MIN_DISTANCE)
                    {
                        // Move towards player
                        moveDir = Vector3.Normalize(toTarget);
                    }

                    // Shoot only if not retreating and in range
                    if (distanceToPlayer < BOT_SHOOT_RANGE && bot.ShootCooldown <= 0f)
                    {
                        float rotationInRad = bot._rotation * (MathF.PI / 180f);
                        Vector3 forward = new Vector3(MathF.Sin(rotationInRad), 0f, MathF.Cos(rotationInRad));
                        byte newProjectileId = GetNextProjectileId();

                        Projectile projectile = new Projectile(null, newProjectileId, bot._position, forward, bot.id, true);
                        _projectiles.Add(projectile);
                        serverLogic.ShootProjectile(projectile.projectileSpawnPacket);

                        bot.ShootCooldown = BOT_SHOOT_COOLDOWN_TIME;
                    }
                }

                Vector3 repulsion = Vector3.Zero;
                foreach (var otherBot in _bots)
                {
                    if (otherBot == bot) continue;

                    Vector3 offset = bot._position - otherBot._position;
                    float dist = offset.Length();
                    if (dist < BOT_AVOID_RADIUS && dist > 0.001f)
                    {
                        repulsion += Vector3.Normalize(offset) * (BOT_REPULSION_STRENGTH / (dist * dist));
                    }
                }

                //Vector3 totalMove = (moveDir + repulsion) * BOT_SPEED * delta;

                //if (totalMove != Vector3.Zero)
                //{
                //    bot._position += totalMove;
                //}

                Vector3 totalMove = (moveDir + repulsion) * BOT_SPEED * delta;
                if (isRetreating)
                    totalMove *= BOT_RETREAT_SPEED_MULTIPLIER;

                if (totalMove != Vector3.Zero)
                {
                    bot._position += totalMove;
                }

                if (distanceToPlayer < BOT_SHOOT_RANGE && bot.ShootCooldown <= 0f)
                {
                    Vector3 origin = bot._position;
                    //toTarget.Y = 0f; // Flatten to XZ plane
                    //Vector3 directionToPlayer = Vector3.Normalize(toTarget);
                    float rotationInRad = bot._rotation * (MathF.PI / 180f);
                    Vector3 forward = new Vector3(MathF.Sin(rotationInRad), 0f, MathF.Cos(rotationInRad));
                    byte newProjectileId = GetNextProjectileId();

                    Projectile projectile = new Projectile(null, newProjectileId, origin, forward, bot.id, true);
                    _projectiles.Add(projectile);
                    serverLogic.ShootProjectile(projectile.projectileSpawnPacket);

                    bot.ShootCooldown = BOT_SHOOT_COOLDOWN_TIME;
                }

                serverLogic.BroadcastBotPosition(bot);
            }
        }
        #endregion
    }
}
