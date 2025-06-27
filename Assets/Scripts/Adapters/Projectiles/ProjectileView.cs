using Core.Projectiles;
using UnityEngine;

namespace Adapters.Projectiles
{
    public class ProjectileView : MonoBehaviour
    {
        public IProjectile _projectile;
        protected GameManager _gameManager;
        public virtual void Setup(IProjectile player, GameManager gameManager)
        {
            _gameManager = gameManager;
            _projectile = player;
            _projectile.OnExpire += () => _gameManager.DisableBullet(this);//Destroy(gameObject);
            enabled = true;
        }

        protected virtual void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _projectile.Position, 0.8f);
        }
    }
}
