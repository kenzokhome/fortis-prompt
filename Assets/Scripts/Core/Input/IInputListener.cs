using System;
using UnityEngine;

namespace Core.Input
{
    public interface IInputListener
    {
        event Action OnShoot;

        Vector2 Movement { get; }
    }
}
