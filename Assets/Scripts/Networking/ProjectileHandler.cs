using Adapters.Projectiles;
using Core.Projectiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fortis.LAN
{
    public struct ProjectileHandler
    {
        public Projectile Player;
        public ProjectileView View;

        public ProjectileHandler(Projectile player, ProjectileView view)
        {
            Player = player;
            View = view;
        }
    }
}