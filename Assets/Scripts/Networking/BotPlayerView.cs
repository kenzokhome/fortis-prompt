using Adapters.Character;
using Core.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Fortis.LAN
{
    public class BotPlayerView : PlayerView
    {
        BotPlayer botEntity;
        public override void Setup(IPlayer player)
        {
            base.Setup(player);
            botEntity = (BotPlayer)player;
        }

        protected override void Update()
        {
            //_remotePlayer.Update(Time.deltaTime);
            float lerpT = ClientLogic.LogicTimer.LerpAlpha;
            transform.position = Vector3.Lerp(transform.position, botEntity.Position, lerpT);
            //transform.rotation = Quaternion.Slerp(transform.rotation, botEntity.Rotation, lerpT);
            transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.Euler(0, botEntity.Rotation, 0),
                    lerpT
                );
        }
    }
}