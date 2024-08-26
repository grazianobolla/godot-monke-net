using MonkeNet.Serializer;

namespace MonkeNet.Shared;

/// <summary>
/// Implement this interface on your entity state IPackableMessage
/// </summary>
public interface IEntityStateMessage : IPackableElement
{
    public int EntityId { get; }
}