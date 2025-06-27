using Adapters.Character;
using Adapters.Input;
using Adapters.Projectiles;
using Codice.Client.BaseCommands.BranchExplorer;
using Core.Player;
using Core.Projectiles;
using Fortis.ObjectPool;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;
namespace Adapters
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] public InputListener _inputListener;
        public UnityEvent<GameObject> warmEvent;
        public GameObject prefab;
        public Transform root;
        public int poolSize = 50;
        private Dictionary<GameObject, ProjectileView> localUsedPool;
        public List<ProjectileView> instantiatedList;

        private void Start()
        {
            InitializeBulletPool();
        }

        public PlayerView SpawnPlayer(byte id, Vector3 position, bool client = false, bool bot = false)
        {
            Player _player = new Player(id, _inputListener, "name");
            PlayerView view = null;
            if (bot == false)
            {
                if (client == true)
                {
                    view = Instantiate(Resources.Load<GameObject>("ClientPlayer").GetComponentInChildren<PlayerView>());
                }
                else
                {
                    view = Instantiate(Resources.Load<GameObject>("RemotePlayer").GetComponentInChildren<PlayerView>());
                }
            }
            else
            {
                view = Instantiate(Resources.Load<GameObject>("BotPlayer").GetComponentInChildren<PlayerView>());
            }
            view.mat = view.GetComponent<MeshRenderer>().material;
            _player.Position = position;
            return view;
        }

        public void ObjectInstantiationEvent(GameObject go)
        {
            go.transform.localPosition = Vector3.zero;
            ProjectileView projectileView = go.GetComponent<ProjectileView>();
            if (projectileView != null)
            {
                instantiatedList.Add(projectileView);
            }
        }

        public void InitializeBulletPool()
        {
            prefab = Resources.Load<GameObject>("ServerProjectile");
            prefab.name = prefab.name + " " + prefab.GetInstanceID();
            instantiatedList = new List<ProjectileView>();

            warmEvent.AddListener(ObjectInstantiationEvent);
            localUsedPool = new Dictionary<GameObject, ProjectileView>();
            PoolManager.WarmPool(prefab, poolSize, root, warmEvent);
        }

        public ProjectileView SpawnObject(Vector3 position, Vector3 direction)
        {
            Projectile projectile = new Projectile(position, direction);
            GameObject go = PoolManager.SpawnObject(prefab, position, Quaternion.identity, null);
            go.transform.parent = null;
            ProjectileView pv = go.GetComponent<ProjectileView>();
            if(!localUsedPool.TryGetValue(go, out var handler))
            {
                localUsedPool.Add(go, pv);
            }
            return pv;
        }

        public void DisableBullet(ProjectileView projectile)
        {
            GameObject go = projectile.gameObject;
            PoolManager.ReleaseObject(go);
            go.transform.parent = root;
            //bullet.transform.position = root.transform.position;//Vector3.zero;
            projectile.transform.localPosition = Vector3.zero;
            localUsedPool.Remove(go);
        }

        public void DestroyPool()
        {
            foreach (var kvp in localUsedPool)
            {
                PoolManager.ReleaseObject(kvp.Key);
                Destroy(kvp.Value);
            }
            PoolManager.RemovePrefab(prefab);
            PoolManager.PrintStatus();
        }
    }
}
