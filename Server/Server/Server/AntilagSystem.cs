using Logic.Packets;
using System;
using System.Numerics;

namespace Server
{
    public struct StateInfo
    {
        public Vector3 Position;
    }

    public class AntilagSystem
    {
        private readonly Dictionary<int, StateInfo>[] _storedPositions;
        private readonly Dictionary<int, StateInfo> _savedStates;
        private int _currentArrayPos;
        private ushort _lastTick;
        private readonly int _maxTicks;

        public AntilagSystem(int maxTicks, int maxPlayers)
        {
            int dictSize = (maxPlayers + 1) * 3;

            _maxTicks = maxTicks;
            _storedPositions = new Dictionary<int, StateInfo>[maxTicks];
            _savedStates = new Dictionary<int, StateInfo>(dictSize);

            for (int i = 0; i < _storedPositions.Length; i++)
            {
                _storedPositions[i] = new Dictionary<int, StateInfo>(dictSize);
            }
        }

        private Dictionary<int, StateInfo> GetStates(ushort tick)
        {
            if (tick < _lastTick - _maxTicks || _lastTick < _maxTicks)
                return null;
            return _storedPositions[(_currentArrayPos - _lastTick + tick - 1) % _maxTicks];
        }

        public void StorePositions(ushort serverTick, List<OnlinePlayer> players)
        {
            var currentDict = _storedPositions[_currentArrayPos];
            currentDict.Clear();

            foreach (var p in players)
            {
                if (p.isDead)
                    continue;
                StateInfo si = new StateInfo
                {
                    Position = p._position
                };
                currentDict.Add(p.AssociatedPeer.Id, si);
            }

            _lastTick = serverTick;
            _currentArrayPos = (_currentArrayPos + 1) % _maxTicks;
        }

        public bool TryApplyAntilag(List<OnlinePlayer> players, ushort tick, int exceptId)
        {
            var antilagStates = GetStates(tick);
            if (antilagStates == null)
                return false;

            _savedStates.Clear();

            foreach (var p in players)
            {
                int id = p.AssociatedPeer.Id;
                if (id == exceptId)
                    continue;
                //Save current states
                StateInfo state = new StateInfo
                {
                    Position = p._position
                };
                //Console.WriteLine("Save state {0} = {1} {2}", id, state.Position, state.Pose);
                _savedStates[id] = state;

                //Apply antilag
                StateInfo antilagState;
                if (antilagStates.TryGetValue(id, out antilagState))
                {
                    //serverController.Player.ChangeState(antilagState.Position, antilagState.Pose, true);
                }
            }

            return true;
        }

        public void RevertAntilag(List<OnlinePlayer> players)
        {
            //Revert states
            foreach (var p in players)
            {
                StateInfo state;
                if (_savedStates.TryGetValue(p.AssociatedPeer.Id, out state))
                {
                    //Console.WriteLine("Load state {0} = {1} {2}", serverController.ServerId, state.Position, state.Pose);
                    //p.ChangeState(state.Position, state.Pose, true);
                }
            }
        }
    }
}
