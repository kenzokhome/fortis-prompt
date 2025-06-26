using Core.Input;
using Core.Player;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Fortis.LAN
{
    public class BotPlayer : Player
    {
        public BotPlayer(byte id) : base(id, null, "name")
        {
            Id = id;
        }

        public override void Update(float deltaTime)
        {

        }
    }
}