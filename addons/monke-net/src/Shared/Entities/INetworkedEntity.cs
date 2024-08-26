namespace MonkeNet.Shared;

/// <summary>
/// All entities, client/server side implement this interface, it contains useful information about entity type, ownership, etc.
/// </summary>
public interface INetworkedEntity
{
    public int EntityId { get; set; }
    public byte EntityType { get; set; }
    public int Authority { get; set; }
}