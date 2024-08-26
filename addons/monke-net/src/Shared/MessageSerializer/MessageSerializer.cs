using MonkeNet.NetworkMessages;
using System.Collections.Generic;
using System.IO;

namespace MonkeNet.Serializer;

public interface IPackableMessage
{
    public void WriteBytes(MessageWriter writer);
    public void ReadBytes(MessageReader reader);
}

public interface IPackableElement : IPackableMessage
{
    public IPackableElement GetCopy();
}

public class MessageSerializer
{
    public static readonly Dictionary<byte, IPackableMessage> Types = [];

    /// <summary>
    /// Takes a IPackableMessage <paramref name="message"/> and packs it into a byte array as <paramref name="messageType"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static byte[] Serialize(MessageTypeEnum messageType, IPackableMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new MessageWriter(stream);

        writer.Write((byte)messageType);
        message.WriteBytes(writer);
        return stream.ToArray();
    }

    /// <summary>
    /// Reads from a byte array <paramref name="bin"/> and produces an IPackableMessage.
    /// </summary>
    /// <param name="bin"></param>
    /// <returns></returns>
    public static IPackableMessage Deserialize(byte[] bin)
    {
        using var stream = new MemoryStream(bin);
        using var reader = new MessageReader(stream);

        byte typeByte = reader.ReadByte();

        // Get instance of the message and "fill it"
        IPackableMessage instance = Types[typeByte];
        instance.ReadBytes(reader);

        // Return the struct, essentialy creating a copy of it (in c# structs are passed by value)
        return instance;
    }
}