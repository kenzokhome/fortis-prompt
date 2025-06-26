using Adapters.Character;
using Core.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fortis.LAN
{
    public struct PlayerHandler
    {
        public readonly Player Player;
        public readonly PlayerView View;

        public PlayerHandler(Player player, PlayerView view)
        {
            Player = player;
            View = view;
        }
    }
}