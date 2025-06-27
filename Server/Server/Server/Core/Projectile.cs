using LiteNetLib;
using Logic.Packets;
using Server.Utils;
using System.Numerics;

namespace Server.Core
{
    public class Projectile
    {
        public NetPeer AssociatedPeer;
        public byte id;
        public string name;
        public Vector3 _position;
        public Vector3 _direction;
        public Vector3 _lastPosition;
        public byte _ownerId;
        public bool isAlive = true;
        private const float MovementSpeed = 8f;
        public ProjectileMovementPacket projectileMovementPackets = new ProjectileMovementPacket();
        public ProjectileSpawnPacket projectileSpawnPacket;
        private GameTimer _shootTimer = new GameTimer(4f);
        public bool isBot;

        public Projectile(NetPeer peer, byte projectileId, Vector3 position, Vector3 direction, byte ownerID,bool isBot)
        {
            id = projectileId;
            AssociatedPeer = peer;
            _position = position;
            _ownerId = ownerID;
            _direction = direction;
            this.isBot = isBot;
            projectileSpawnPacket = new ProjectileSpawnPacket
            {
                ProjectileId = projectileId,
                Position = position,
                Direction = direction,
                OwnerId = ownerID,
                isBot = isBot,
            };
        }

        public void Update(float delta)
        {
            if (isAlive == false)
                return;
            _shootTimer.UpdateAsCooldown(delta);
            _lastPosition = _position;
            _position += _direction * (MovementSpeed * delta);
            if (_shootTimer.IsTimeElapsed)
            {
                _shootTimer.Reset();
                isAlive = false;
                return;
            }
        }
    }
}
