using Godot;
using MonkeNet.Server;
using MonkeNet.Shared;

namespace GameDemo;

public partial class ServerPlayer : CharacterBody3D, INetworkedEntity, IServerEntity
{
    public int EntityId { get; set; }
    public byte EntityType { get; set; }
    public int Authority { get; set; }
    public float Yaw { get; set; }

    // Capture current entity state, sent by the Server Entity Manager to all clients
    public IEntityStateMessage GenerateCurrentStateMessage()
    {
        return new EntityStateMessage
        {
            EntityId = this.EntityId,
            Yaw = this.Yaw,
            Position = this.Position,
            Velocity = this.Velocity
        };
    }
}
