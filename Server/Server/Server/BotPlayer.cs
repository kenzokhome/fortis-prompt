using Logic.Packets;
using Server.Networking;
using Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class BotPlayer : Player
    {
        public BotMovementPacket cachedBotMovementPacket;

        public float ShootCooldown;
        private float respawnTimer = 0f;
        private const float respawnDelay = 5f;

        private readonly RoomManager roomManager; // reference to your server logic for position generation

        public BotPlayer(byte id, Vector3 startPos, RoomManager roomManager) : base("", id)
        {
            this.id = id;
            _position = startPos;
            ShootCooldown = 0f;
            _health = 100;
            cachedBotMovementPacket = new BotMovementPacket();
            this.roomManager = roomManager;
        }

        public override bool Hit(Projectile projectile)
        {
            bool hasHit = false;
            if (isDead) return false;
            if (id != projectile._ownerId)
            {
                if (projectile.isAlive)
                {
                    bool hit = Collisions.CheckIntersection(
                        projectile._lastPosition.X, projectile._lastPosition.Y, projectile._lastPosition.Z,
                        projectile._position.X, projectile._position.Y, projectile._position.Z,
                        this
                    );
                    if (hit)
                    {
                        _health -= 10;
                        if (_health <= 0)
                        {
                            isDead = true;
                            respawnTimer = respawnDelay; // start respawn countdown
                        }
                        hasHit = true;
                    }
                }
            }
            return hasHit;
        }

        public override void Update(float delta)
        {
            if (isDead)
            {
                respawnTimer -= delta;
                if (respawnTimer <= 0f)
                {
                    roomManager.Respawn(this);
                }
            }
        }
    }
}
