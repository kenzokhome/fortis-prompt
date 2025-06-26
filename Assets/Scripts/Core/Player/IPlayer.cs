using System;
using UnityEngine;

namespace Core.Player
{
    public interface IPlayer : IDisposable
    {
        event OnShootHandler OnShoot;

        Vector3 Position { get; set; }
        float Rotation { get; set; }

        //void Tick(Vector2 input, float delta);
        void Update(float delta);
    }
}
