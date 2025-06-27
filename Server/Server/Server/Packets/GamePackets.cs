using LiteNetLib.Utils;
using System.Numerics;
using Server.Utils;

namespace Logic.Packets
{
    public enum PacketType : byte
    {
        Movement,
        Spawn,
        ServerState,
        Serialized,
        Shoot,
        ProjectileSpawn,
        ProjectileMovement,
        BotMovement
    }

    //Auto serializable packets
    public class JoinPacket
    {
        public string? UserName { get; set; }
    }

    public class JoinAcceptPacket
    {
        public byte Id { get; set; }
        public Vector3 Position { get; set; }
        public ushort ServerTick { get; set; }
    }

    public class PlayerReadyPacket
    {
        public byte Id { get; set; }
        public bool IsReady { get; set; }
    }

    public class SessionStartPacket
    {

    }

    public class PlayerResetPacket
    {
        public byte Id { get; set; }
    }

    public class PlayerJoinedPacket
    {
        public byte Id { get; set; }
        public Vector3 Position { get; set; }
        public string UserName { get; set; }
        public bool NewPlayer { get; set; }
        public float Health { get; set; }
        public ushort ServerTick { get; set; }
        public PlayerState InitialPlayerState { get; set; }
    }

    public class PlayerLeavedPacket
    {
        public byte Id { get; set; }
    }

    public class BotSpawnPacket
    {
        public byte Id { get; set; }
        public Vector3 Position { get; set; }
    }

    //Manual serializable packets
    public struct BotMovementPacket : INetSerializable
    {
        public byte botId;
        public Vector3 Position;
        public float Rotation;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(botId);
            writer.PutV3(Position);
            writer.Put(Rotation);
        }

        public void Deserialize(NetDataReader reader)
        {
            botId = reader.GetByte();
            Position = reader.GetVector3();
            Rotation = reader.GetFloat();
        }
    }

    public struct ProjectileSpawnPacket : INetSerializable
    {
        public byte ProjectileId;
        public Vector3 Position;
        public Vector3 Direction;
        public byte OwnerId;
        public bool isBot;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProjectileId);
            writer.PutV3(Position);
            writer.PutV3(Direction);
            writer.Put(OwnerId);
            writer.Put(isBot);
        }

        public void Deserialize(NetDataReader reader)
        {
            ProjectileId = reader.GetByte();
            Position = reader.GetVector3();
            Direction = reader.GetVector3();
            OwnerId = reader.GetByte();
            isBot = reader.GetBool();
        }
    }

    public struct ProjectileMovementPacket : INetSerializable
    {
        public byte ProjectileId;
        public Vector3 Position;
        public byte OwnerId;
        public bool isBot;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ProjectileId);
            writer.PutV3(Position);
            writer.Put(OwnerId);
            writer.Put(isBot);
        }

        public void Deserialize(NetDataReader reader)
        {
            ProjectileId = reader.GetByte();
            Position = reader.GetVector3();
            OwnerId = reader.GetByte();
            isBot = reader.GetBool();
        }
    }

    public class ProjectileDestroyPacket
    {
        public byte Id { get; set; }
        public int OwnerId { get; set; }
        public int hitPlayerId { get; set; }
        public bool isBot { get; set; }
    }

    public class ShootPacket : INetSerializable
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public byte FromPlayerId;

        public void Serialize(NetDataWriter writer)
        {
            writer.PutV3(Origin);
            writer.PutV3(Direction);
            writer.Put(FromPlayerId);
        }

        public void Deserialize(NetDataReader reader)
        {
            Origin = reader.GetVector3();
            Direction = reader.GetVector3();
            FromPlayerId = reader.GetByte();
        }
    }

    public class HealthUpdatePacket
    {
        public byte PlayerId { get; set; }
        public float Health { get; set; }
        public bool isBot { get; set; }
    }

    public struct PlayerInputPacket : INetSerializable
    {
        public ushort Id;
        public Vector2 input;
        public float Rotation;
        public ushort ServerTick;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.PutV2(input);
            writer.Put(Rotation);
            writer.Put(ServerTick);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetUShort();
            input = reader.GetVector2();
            Rotation = reader.GetFloat();
            ServerTick = reader.GetUShort();
        }
    }

    public struct PlayerState : INetSerializable
    {
        public int Id;
        public Vector3 Position;
        public float Rotation;
        public ushort Tick;

        public const int Size = 1 + 8 + 4 + 2;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Id);
            writer.PutV3(Position);
            writer.Put(Rotation);
            writer.Put(Tick);
        }

        public void Deserialize(NetDataReader reader)
        {
            Id = reader.GetInt();
            Position = reader.GetVector3();
            Rotation = reader.GetFloat();
            Tick = reader.GetUShort();
        }
    }

    public struct ServerState : INetSerializable
    {
        public ushort Tick;
        public ushort LastProcessedCommand;

        public int PlayerStatesCount;
        public int StartState; //server only
        public PlayerState[] PlayerStates;

        //tick
        public const int HeaderSize = sizeof(ushort) * 2;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            writer.Put(LastProcessedCommand);

            for (int i = 0; i < PlayerStatesCount; i++)
                PlayerStates[StartState + i].Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetUShort();
            LastProcessedCommand = reader.GetUShort();

            PlayerStatesCount = reader.AvailableBytes / PlayerState.Size;
            if (PlayerStates == null || PlayerStates.Length < PlayerStatesCount)
                PlayerStates = new PlayerState[PlayerStatesCount];
            for (int i = 0; i < PlayerStatesCount; i++)
                PlayerStates[i].Deserialize(reader);
        }
    }
}
