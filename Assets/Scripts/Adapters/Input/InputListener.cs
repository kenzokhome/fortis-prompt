using System;
using Core.Input;
using UnityEngine;

namespace Adapters.Input
{
    public class InputListener : MonoBehaviour, IInputListener
    {
        public event Action OnShoot;

        public Vector2 Movement { get; private set; }

        public float fireRate = 0.5f;
        private float lastFireTime = 0f;

        private void Update()
        {
            if (UnityEngine.Input.GetKey(KeyCode.Space) && Time.time >= lastFireTime + fireRate)
            {
                lastFireTime = Time.time;
                OnShoot?.Invoke();
            }

            Movement = new Vector2(UnityEngine.Input.GetAxis("Horizontal"), UnityEngine.Input.GetAxis("Vertical"));
        }
    }
}
