using Adapters;
using Adapters.Projectiles;
using Core.Projectiles;
using LiteNetLib;
using Logic.Packets;
using UnityEngine;

namespace Fortis.LAN
{
    public class ServerProjectileView : ProjectileView
    {
        public override void Setup(IProjectile player, GameManager gameManager)
        {
            _gameManager = gameManager;
            _projectile = player;
            _projectile.OnExpire += () => _gameManager.DisableBullet(this);//Destroy(gameObject);
            enabled = true;
        }

        protected override void Update()
        {
            float lerpT = ClientLogic.LogicTimer.LerpAlpha;
            transform.position = Vector3.Lerp(transform.position, _projectile.Position, lerpT);
        }
    }
}
