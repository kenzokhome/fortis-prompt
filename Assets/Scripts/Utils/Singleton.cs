using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fortis.Utils
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        public static T instance { get; set; }

        public virtual void Awake()
        {
            instance = this as T;
        }
    }


    public class PersistentSingleton<T> : MonoBehaviour where T : Component
    {
        public static T instance { get; set; }

        public virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(instance.gameObject);
            }
            else
            {
                T thisScript = this as T;
                Destroy(thisScript.gameObject);
            }
        }
    }
}
