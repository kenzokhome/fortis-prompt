using Core.Projectiles;
using Fortis.LAN;
using System;
using UnityEngine;

namespace Fortis.LAN
{
    public class ServerProjectile : Projectile
    {
        public ServerProjectile(Vector3 initialPosition, Vector3 direction) : base(initialPosition, direction)
        {
            Position = initialPosition;
            _direction = direction.normalized;
        }
    }
}
