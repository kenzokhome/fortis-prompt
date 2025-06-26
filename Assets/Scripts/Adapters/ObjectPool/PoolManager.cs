using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Fortis.ObjectPool
{
    /// <summary>
    /// DOES NOT CLEAR ON RECONNECT
    /// </summary>
    public static class PoolManager
    {
        //public Transform root;
        private static Dictionary<GameObject, ObjectPoolContainer<GameObject>> prefabLookup = new Dictionary<GameObject, ObjectPoolContainer<GameObject>>();
        private static Dictionary<GameObject, ObjectPoolContainer<GameObject>> prefabInUseLookup = new Dictionary<GameObject, ObjectPoolContainer<GameObject>>();

        public static bool dirty = false;
        public static bool logStatus = false;

        private static int counter = 0;
        public static void InitialisePoolManger()
        {
            prefabLookup = new Dictionary<GameObject, ObjectPoolContainer<GameObject>>();
            prefabInUseLookup = new Dictionary<GameObject, ObjectPoolContainer<GameObject>>();
        }

        public static void PoolManagerUpdate()
        {
            if (logStatus && dirty)
            {
                //PrintStatus();
                dirty = false;
            }
        }

        private static void warmPool(GameObject prefab, int size, Transform root, UnityEvent<GameObject> warmEvent)
        {
            if (prefabLookup.ContainsKey(prefab))
            {
                throw new Exception("Pool for prefab " + prefab.name + " has already been created");
            }
            var container = new ObjectPoolContainer<GameObject>(() => { return InstantiatePrefab(prefab, root); }, size, warmEvent);
            prefabLookup[prefab] = container;
            dirty = true;
        }

        private static GameObject spawnObject(GameObject prefab, Transform root)
        {
            return spawnObject(prefab, Vector3.zero, Quaternion.identity, root);
        }

        private static GameObject spawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform root)
        {
            if (!prefabLookup.ContainsKey(prefab))
            {
                Debug.LogWarning("No prefab found on: " + root.gameObject.name);
                return null;
            }

            var container = prefabLookup[prefab];

            var clone = container.GetItem();
            clone.transform.SetPositionAndRotation(position, rotation);
            clone.SetActive(true);

            prefabInUseLookup.Add(clone, container);
            dirty = true;
            return clone;
        }

        private static void releaseObject(GameObject clone, bool shouldDisable)
        {
            if (shouldDisable)
            {
                clone.SetActive(false);
            }

            if (prefabInUseLookup.ContainsKey(clone))
            {
                prefabInUseLookup[clone].ReleaseItem(clone);
                prefabInUseLookup.Remove(clone);
                dirty = true;
            }
            else
            {
                Debug.LogWarning("No pool contains the object: " + clone.name);
            }
        }

        public static void RemovePrefab(GameObject prefab)
        {
            if (prefabLookup.ContainsKey(prefab))
            {
                prefabLookup[prefab].ReleaseItem(prefab);
                prefabLookup.Remove(prefab);
            }
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform root)
        {
            var go = GameObject.Instantiate(prefab) as GameObject;
            if (root != null) go.transform.parent = root;
            go.name = go.name + " " + counter;
            counter++;
            return go;
        }

        public static void PrintStatus()
        {
            foreach (KeyValuePair<GameObject, ObjectPoolContainer<GameObject>> keyVal in prefabLookup)
            {
                Debug.Log(string.Format("Object Pool for Prefab: {0} In Use: {1} Total {2}", keyVal.Key.name, keyVal.Value.CountUsedItems, keyVal.Value.Count));
            }
            foreach (KeyValuePair<GameObject, ObjectPoolContainer<GameObject>> keyVal in prefabInUseLookup)
            {
                Debug.Log(string.Format("Object Pool for Instance: {0} In Use: {1} Total {2}", keyVal.Key.name, keyVal.Value.CountUsedItems, keyVal.Value.Count));
            }
        }

        #region Static API

        public static void WarmPool(GameObject prefab, int size, Transform root, UnityEvent<GameObject> warmEvent = null)
        {
            warmPool(prefab, size, root, warmEvent);
        }

        public static GameObject SpawnObject(GameObject prefab, Transform root)
        {
            return spawnObject(prefab, root);
        }

        public static GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform root)
        {
            return spawnObject(prefab, position, rotation, root);
        }

        public static void ReleaseObject(GameObject clone, bool shouldDisable = true)
        {
            releaseObject(clone, shouldDisable);
        }

        #endregion
    }
}