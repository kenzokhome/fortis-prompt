using Logic.Packets;
using Server.Utils;
using System.Numerics;

namespace Server.Core
{
    public class Player
    {
        public byte id;
        public string name;
        public Vector3 _position; 
        public float _rotation;
        public float _health;
        protected const float MovementSpeed = 4f;
        protected const float RotationSpeed = 0.25f;
        protected Vector3 _lastMovementDirection;
        public bool isDead = false;
        public bool isReady = false;

        public Player(string name, byte id)
        {
            this.id = id;
            this.name = name;
        }

        public virtual void Spawn(Vector3 position)
        {
            _position = position;
            _rotation = 0.0f;
            _health = 100;
        }

        public virtual void Tick(PlayerInputPacket command, float delta)
        {
            Vector3 direction = new Vector3(command.input.X, 0f, command.input.Y);

            Vector3 movement = direction * (MovementSpeed * delta);//Time.fixedDeltaTime);
            _position += movement;

            if (movement == Vector3.Zero)
            {
                return;
            }

            _lastMovementDirection = movement;
            _rotation = command.Rotation;
            //_rotation = Quaternion.Slerp(
            //    _rotation,
            //    command.Rotation,
            //    RotationSpeed
            //);
        }

        public virtual bool Hit(Projectile projectile)
        {
            if (isDead) return false;
            bool hasHit = false;
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
                            isDead = true;
                        hasHit = true;
                    }
                }
            }
            return hasHit;
        }

        public virtual void Update(float delta)
        {

        }
    }
}
