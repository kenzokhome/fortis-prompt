using System;
using System.Collections.Generic;
using Core.Input;
using Core.Projectiles;
using UnityEngine;

namespace Core.Player
{
    //public delegate void OnShootHandler(Vector3 origin, Vector3 direction);
    public delegate void OnShootHandler();

    public class Player : IDisposable, IPlayer
    {
        public float _health;
        public bool isDead = false;
        public readonly string Name;

        protected const float MovementSpeed = 4f;
        protected const float RotationSpeed = 0.25f;

        public event OnShootHandler OnShoot;

        protected IInputListener _inputListener;
        protected Vector3 _lastMovementDirection;

        protected Vector3 _position = new Vector3(0, 1, 0);

        protected float _rotation;// { get; private set; }

        public Transform transform;

        public List<Projectile> projectiles = new List<Projectile>();
        public byte Id { get; set; }
        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        public float Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
            }
        }

        public Player(byte id, IInputListener inputListener, string name)
        {
            Id = id;
            _inputListener = inputListener;
            Name = name;
        }

        public virtual void Spawn(Vector3 position)
        {
            _position = position;
            _rotation = 0.0f;
            _health = 100;
        }

        //protected virtual void HandleShoot() => OnShoot?.Invoke(Position, _lastMovementDirection);
        protected virtual void HandleShoot() => OnShoot?.Invoke();

        public virtual void Update(float delta)
        {

        }

        public void Dispose()
        {
            if (_inputListener != null)
                _inputListener.OnShoot -= HandleShoot;
        }
    }
}
