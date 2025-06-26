using LiteNetLib;
using Logic.Packets;
using Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class OnlinePlayer : Player
    {
        public readonly NetPeer AssociatedPeer;
        public PlayerState NetworkState;
        public int Ping;
        public ushort LastProcessedCommandId { get; private set; }

        public OnlinePlayer(string name, NetPeer peer, Vector3 pos) : base(name, (byte)peer.Id)
        {
            peer.Tag = this;
            AssociatedPeer = peer;
            id = (byte)peer.Id;
            NetworkState = new PlayerState { Id = (byte)peer.Id };
            Spawn(pos);
        }

        public void ApplyInput(PlayerInputPacket command, float delta)
        {
            if (NetworkGeneral.SeqDiff(command.Id, LastProcessedCommandId) <= 0)
                return;
            LastProcessedCommandId = command.Id;
            base.Tick(command, delta);
        }

        public override void Update(float delta)
        {
            base.Update(delta);
            NetworkState.Position = _position;
            NetworkState.Rotation = _rotation;
            NetworkState.Tick = LastProcessedCommandId;
        }
    }
}
