using Adapters;
using Adapters.Character;
using Core.Input;
using Fortis.LAN;
using LiteNetLib;
using Logic.Packets;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
namespace Core.Player
{
    public class ClientPlayer : Player
    {
        public int StoredCommands => _predictionPlayerStates.Count;
        public Vector2 LastPosition { get; private set; }
        public float LastRotation { get; private set; }

        private PlayerInputPacket _nextCommand;
        private ServerState _lastServerState;
        private readonly ClientLogic _clientLogic;

        private const int MaxStoredCommands = 60;
        private bool _firstStateReceived;
        private int _updateCount;
        private readonly LiteRingBuffer<PlayerInputPacket> _predictionPlayerStates;

        public ClientPlayer(ClientLogic clientLogic, byte id, IInputListener inputListener, string name) : base(id, inputListener, name)
        {
            _clientLogic = clientLogic;
            _predictionPlayerStates = new LiteRingBuffer<PlayerInputPacket>(MaxStoredCommands);
            _inputListener = inputListener;
            if (_inputListener != null)
            {
                _inputListener.OnShoot += TryShoot;
            }
            this.Id = id;
        }

        public void TryShoot()
        {
            _clientLogic.TryShoot(this);
        }

        public void Tick(PlayerInputPacket command, float delta)
        {
            Vector3 direction = new Vector3(command.input.x, 0f, command.input.y);
            Vector3 movement = direction * (MovementSpeed * delta);
            Position += movement;

            if (movement == Vector3.zero)
            {
                return;
            }

            _lastMovementDirection = movement;
            Rotation = command.Rotation;
        }

        public void ReceiveServerState(ServerState serverState, PlayerState ourState)
        {
            if (!_firstStateReceived)
            {
                if (serverState.LastProcessedCommand == 0)
                    return;
                _firstStateReceived = true;
            }
            if (serverState.Tick == _lastServerState.Tick ||
                serverState.LastProcessedCommand == _lastServerState.LastProcessedCommand)
                return;

            _lastServerState = serverState;

            //sync
            _position = ourState.Position;
            _rotation = ourState.Rotation;
            if (_predictionPlayerStates.Count == 0)
                return;

            ushort lastProcessedCommand = serverState.LastProcessedCommand;
            int diff = NetworkGeneral.SeqDiff(lastProcessedCommand, _predictionPlayerStates.First.Id);

            //apply prediction
            if (diff >= 0 && diff < _predictionPlayerStates.Count)
            {
                //Debug.Log($"[OK]  SP: {serverState.LastProcessedCommand}, OUR: {_predictionPlayerStates.First.Id}, DF:{diff}");
                _predictionPlayerStates.RemoveFromStart(diff + 1);
                foreach (var state in _predictionPlayerStates)
                    Tick(state, LogicTimer.FixedDelta);

            }
            else if (diff >= _predictionPlayerStates.Count)
            {
                Debug.Log($"[C] Player input lag st: {_predictionPlayerStates.First.Id} ls:{lastProcessedCommand} df:{diff}");
                //lag
                _predictionPlayerStates.FastClear();
                _nextCommand.Id = lastProcessedCommand;
            }
            else
            {
                Debug.Log($"[ERR] SP: {serverState.LastProcessedCommand}, OUR: {_predictionPlayerStates.First.Id}, DF:{diff}, STORED: {StoredCommands}");
            }
        }

        public override void Update(float delta)
        {
            LastPosition = _position;
            LastRotation = _rotation;

            _nextCommand.Id = (ushort)((_nextCommand.Id + 1) % NetworkGeneral.MaxGameSequence);
            _nextCommand.ServerTick = _lastServerState.Tick;
            _nextCommand.input = _clientLogic.gameManager._inputListener.Movement.normalized;
            _nextCommand.Rotation = Mathf.Atan2(_nextCommand.input.x, _nextCommand.input.y) * Mathf.Rad2Deg;
            Tick(_nextCommand, delta);
            if (_predictionPlayerStates.IsFull)
            {
                _nextCommand.Id = (ushort)(_lastServerState.LastProcessedCommand + 1);
                _predictionPlayerStates.FastClear();
            }
            _predictionPlayerStates.Add(_nextCommand);

            _updateCount++;
            if (_updateCount == 3)
            {
                _updateCount = 0;
                foreach (var t in _predictionPlayerStates)
                    _clientLogic.SendPacketSerializable(PacketType.Movement, t, DeliveryMethod.Unreliable);
            }

            base.Update(delta);
        }
    }

    public class RemotePlayer : Player
    {
        private readonly LiteRingBuffer<PlayerState> _buffer = new LiteRingBuffer<PlayerState>(30);
        private float _receivedTime;
        private float _timer;
        private const float BufferTime = 0.1f; //100 milliseconds
        public Vector2 LastPosition { get; private set; }
        public float LastRotation { get; private set; }

        public RemotePlayer(PlayerJoinedPacket packet, byte id, string name) : base(id, null, name)
        {
            _position = packet.InitialPlayerState.Position;
            _health = packet.Health;
            _rotation = packet.InitialPlayerState.Rotation;
            _buffer.Add(packet.InitialPlayerState);
        }

        public override void Spawn(Vector3 position)
        {
            _buffer.FastClear();
            base.Spawn(position);
        }

        public override void Update(float delta)
        {
            if (_receivedTime < BufferTime || _buffer.Count < 2)
                return;
            var dataA = _buffer[0];
            var dataB = _buffer[1];
            float lerpTime = NetworkGeneral.SeqDiff(dataB.Tick, dataA.Tick) * LogicTimer.FixedDelta;
            float t = _timer / lerpTime;
            _position = Vector3.Lerp(dataA.Position, dataB.Position, t);
            //if (_position.sqrMagnitude > 0.001f)
            //    _rotation = Quaternion.Slerp(dataA.Rotation, dataB.Rotation, t);
            _rotation = Mathf.LerpAngle(dataA.Rotation, dataB.Rotation, t);
            _timer += delta;
            if (_timer > lerpTime)
            {
                _receivedTime -= lerpTime;
                _buffer.RemoveFromStart(1);
                _timer -= lerpTime;
            }
        }

        public void OnPlayerState(PlayerState state)
        {
            //old command
            int diff = NetworkGeneral.SeqDiff(state.Tick, _buffer.Last.Tick);
            if (diff <= 0)
                return;
            _receivedTime += diff * LogicTimer.FixedDelta;
            if (_buffer.IsFull)
            {
                Debug.LogWarning("[C] Remote: Something happened");
                //Lag?
                _receivedTime = 0f;
                _buffer.FastClear();
            }
            _buffer.Add(state);
        }
    }
}
