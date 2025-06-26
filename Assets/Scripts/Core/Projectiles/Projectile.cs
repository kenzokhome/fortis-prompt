using System;
using UnityEngine;

namespace Core.Projectiles
{
    public class Projectile : IProjectile
    {
        public event Action OnExpire;

        public int id;
        public int ownerId;

        private const float MovementSpeed = 8f;
        protected const float Duration = 4f;

        protected Vector3 _direction;
        protected float endTime;

        public Vector3 Position { get; set; }
        public bool Expired { get; set; }

        public Projectile(Vector3 initialPosition, Vector3 direction)
        {
            Position = initialPosition;
            _direction = direction.normalized;
            endTime = Time.fixedTime + Duration;
        }

        public void Tick()
        {
            Debug.Log("Triggered to destroy");
            //if (Time.fixedTime > endTime)
            //{
                Expired = true;
                OnExpire?.Invoke();
            //    return;
            //}

            //Position += _direction * (MovementSpeed * Time.fixedDeltaTime);
        }

    }
}
