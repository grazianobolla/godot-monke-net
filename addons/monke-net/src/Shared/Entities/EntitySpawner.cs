using Godot;

namespace MonkeNet.Shared;

public abstract partial class EntitySpawner : Node
{
    protected abstract Node HandleEntityCreationClientSide(int entityId, byte entityType);
    protected abstract Node HandleEntityCreationServerSide(int entityId, byte entityType);

    public static EntitySpawner Instance { get; private set; }

    /// <summary>
    /// Stores a collection of the currently instanced entities
    /// </summary>
    public Godot.Collections.Array<Node> Entities { get; private set; } = [];

    public override void _Ready()
    {
        Instance = this;
    }

    // Can be called from both the server or a client, so it needs to handle both scenarios
    public Node SpawnEntity(int entityId, byte entityType, int authority)
    {
        Node instancedNode;
        if (MonkeNetManager.Instance.IsServer)
        {
            instancedNode = HandleEntityCreationServerSide(entityId, entityType);
        }
        else
        {
            instancedNode = HandleEntityCreationClientSide(entityId, entityType);
        }

        InitializeEntity(instancedNode, entityId, entityType, authority);
        AddChild(instancedNode);
        Entities.Add(instancedNode);
        return instancedNode;
    }

    public void DestroyEntity(int entityId)
    {
        Node node = GetNode(entityId.ToString());
        Entities.Remove(node);
        node.Free();
    }

    private void InitializeEntity(Node node, int entityId, byte entityType, int authority)
    {
        node.Name = entityId.ToString();

        if (node is INetworkedEntity entity)
        {
            entity.EntityId = entityId;
            entity.EntityType = entityType;
            entity.Authority = authority;
        }
    }
}