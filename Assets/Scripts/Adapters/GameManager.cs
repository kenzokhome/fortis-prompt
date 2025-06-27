using Adapters.Character;
using Adapters.Input;
using Adapters.Projectiles;
using Core.Player;
using Core.Projectiles;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;
namespace Adapters
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] public InputListener _inputListener;
        ///private Player _player;
        //private List<Player> _players = new();
        //private List<IProjectile> _projectiles = new();

        public PlayerView SpawnPlayer(byte id, Vector3 position, bool client = false, bool bot = false)
        {
            Player _player = new Player(id, _inputListener, "name");
            //_player.OnShoot += HandleShoot;

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
            //view.Setup(_player);
            //_players.Add(_player);
            return view;
        }

        public ProjectileView HandleShoot(Vector3 position, Vector3 direction)
        {
            Projectile projectile = new Projectile(position, direction);
            ProjectileView projectileView = Instantiate(Resources.Load<ProjectileView>("ServerProjectile"));
            projectileView.transform.position = position;
            //projectileView.Setup(projectile);
            //_projectiles.Add(projectile);
            return projectileView;
        }
    }
}
