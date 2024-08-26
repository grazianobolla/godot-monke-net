using Godot;
using System.IO;

namespace MonkeNet.Serializer;

public class MessageWriter(MemoryStream stream) : BinaryWriter(stream)
{
    /// <summary>
    /// Packs a List of a IPackableMessage into the stream, will also use 4 bytes for the list size
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    public void Write(IPackableMessage[] data)
    {
        Write(data.Length);
        for (int i = 0; i < data.Length; i++) { data[i].WriteBytes(this); }
    }

    public void Write(Vector3 vector)
    {
        Write(vector.X);
        Write(vector.Y);
        Write(vector.Z);
    }
}
