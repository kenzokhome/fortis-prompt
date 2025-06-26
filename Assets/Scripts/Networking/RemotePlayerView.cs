using Adapters.Character;
using Core.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fortis.LAN
{
    public class RemotePlayerView : PlayerView
    {
        public override void Setup(IPlayer player)
        {
            base.Setup(player);
        }

        protected override void Update()
        {
            float lerpT = ClientLogic.LogicTimer.LerpAlpha;
            transform.position = Vector3.Lerp(transform.position, _player.Position, lerpT);
            //transform.rotation = Quaternion.Slerp(transform.rotation, _remotePlayer.Rotation, lerpT);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(0, _player.Rotation, 0),
                lerpT
            );
        }
    }
}