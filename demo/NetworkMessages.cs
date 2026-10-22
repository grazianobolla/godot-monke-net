using Godot;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace GameDemo;

// Entity state sent by the server to all clients every time a snapshot is produced
public struct EntityStateMessage : IEntityStateData
{
    public int EntityId { get; set; } // Entity Id
    public Vector3 Position { get; set; } // Entity Position
    public Vector3 Velocity { get; set; } // Entity velocity
    public Vector3 AngularVelocity { get; set; } // Entity velocity
    public Vector3 Rotation { get; set; }
    public float Yaw { get; set; } // Looking angle
    public byte InputKeys { get; set; } // Last input keys processed by the server, used by remote clients to extrapolate

    public void ReadBytes(MessageReader reader)
    {
        EntityId = reader.ReadInt32();
        Position = reader.ReadVector3();
        Velocity = reader.ReadVector3();
        AngularVelocity = reader.ReadVector3();
        Rotation = reader.ReadVector3();
        Yaw = reader.ReadSingle();
        InputKeys = reader.ReadByte();
    }

    public readonly void WriteBytes(MessageWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Position);
        writer.Write(Velocity);
        writer.Write(AngularVelocity);
        writer.Write(Rotation);
        writer.Write(Yaw);
        writer.Write(InputKeys);
    }

    public readonly IPackableElement GetCopy() => this;
}

// Character inputs sent to the server by a local player every time a key is pressed
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