using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Client;

public partial class ClientEntityManager : ClientNetworkNode
{
    private EntitySpawner _entitySpawner;

    public override void _Ready()
    {
        _entitySpawner = GetNode<EntitySpawner>(MonkeNetManager.Instance.EntitySpawnerNodePath);
        base._Ready();
    }

    /// <summary>
    /// Requests the server to spawn an entity
    /// </summary>
    /// <param name="entityType"></param>
    public void MakeEntityRequest(byte entityType)
    {
        var req = new EntityRequest
        {
            EntityType = entityType
        };

        SendCommandToServer((byte)MessageTypeEnum.EntityRequest, req, INetworkManager.PacketModeEnum.Reliable, (int)ChannelEnum.EntityEvent);
    }

    protected override void OnCommandReceived(IPackableMessage command)
    {
        if (command is EntityEvent entityEvent)
        {
            switch (entityEvent.Event)
            {
                case EntityEventEnum.Created:
                    _entitySpawner.SpawnEntity(entityEvent.EntityId, entityEvent.EntityType, entityEvent.Authority); //TODO: just pass the event directly
                    break;
                case EntityEventEnum.Destroyed:
                    _entitySpawner.DestroyEntity(entityEvent.EntityId); //TODO: just pass the event directly
                    break;
                default:
                    break;
            }
        }
    }
}
