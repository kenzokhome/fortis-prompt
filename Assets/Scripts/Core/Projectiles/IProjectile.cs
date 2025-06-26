using System;
using UnityEngine;

namespace Core.Projectiles
{
    public interface IProjectile
    {
        event Action OnExpire;
        Vector3 Position { get; }
        bool Expired { get; }
        void Tick();
    }
}
