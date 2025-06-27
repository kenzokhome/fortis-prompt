using Core.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fortis.LAN
{
    public class RoomManager
    {
        public Session currentSession = Session.Stop;

        public Dictionary<int, PlayerHandler> _players;
        public Dictionary<int, PlayerHandler> bots;
        public ClientPlayer clientPlayer;
        private readonly ClientLogic _clientLogic;

        public RoomManager(ClientLogic _clientLogic) 
        {
            _players = new Dictionary<int, PlayerHandler>();
            bots = new Dictionary<int, PlayerHandler>();
            this._clientLogic = _clientLogic;
        }
    }
}