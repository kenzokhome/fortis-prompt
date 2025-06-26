using Core.Projectiles;
using UnityEngine;

namespace Adapters.Projectiles
{
    public class ProjectileView : MonoBehaviour
    {
        public IProjectile _projectile;

        public virtual void Setup(IProjectile player)
        {
            _projectile = player;
            _projectile.OnExpire += () => Destroy(gameObject);
            enabled = true;
        }

        protected virtual void Update()
        {
            transform.position = Vector3.Lerp(transform.position, _projectile.Position, 0.8f);
        }
    }
}
