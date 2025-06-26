using System;
using Core.Input;
using UnityEngine;

namespace Adapters.Input
{
    public class InputListener : MonoBehaviour, IInputListener
    {
        public event Action OnShoot;

        public Vector2 Movement { get; private set; }

        private void Update ()
        {
            if (UnityEngine.Input.GetKeyUp(KeyCode.Space))
            {
                OnShoot?.Invoke();
            }

            Movement = new Vector2(UnityEngine.Input.GetAxis("Horizontal"), UnityEngine.Input.GetAxis("Vertical"));
        }
    }
}
