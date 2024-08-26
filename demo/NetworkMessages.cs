using Godot;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace GameDemo;

// Entity state sent by the server to all clients every time a snapshot is produced
[RegisterMessage(MessageTypeEnum.EntityState, typeof(EntityStateMessage))]
public struct EntityStateMessage : IEntityStateMessage
{
    public int EntityId { get; set; } // Entity Id
    public Vector3 Position { get; set; } // Entity Position
    public Vector3 Velocity { get; set; } // Entity velocity
    public float Yaw { get; set; } // Looking angle

    public void ReadBytes(MessageReader reader)
    {
        EntityId = reader.ReadInt32();
        Position = reader.ReadVector3();
        Velocity = reader.ReadVector3();
        Yaw = reader.ReadSingle();
    }

    public readonly void WriteBytes(MessageWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Position);
        writer.Write(Velocity);
        writer.Write(Yaw);
    }

    public readonly IPackableElement GetCopy() => this;
}

// Character inputs sent to the server by a local player every time a key is pressed
[RegisterMessage(MessageTypeEnum.CharacterControllerInput, typeof(CharacterInputMessage))]
public struct CharacterInputMessage : IPackableElement
{
    public byte Keys { get; set; } // Single byte were each bit is a different pressed key
    public float CameraYaw { get; set; } // Yaw (were are we looking at)

    public readonly void WriteBytes(MessageWriter writer)
    {
        writer.Write(Keys);
        writer.Write(CameraYaw);
    }

    public void ReadBytes(MessageReader reader)
    {
        Keys = reader.ReadByte();
        CameraYaw = reader.ReadSingle();
    }

    public readonly IPackableElement GetCopy() => this;
}