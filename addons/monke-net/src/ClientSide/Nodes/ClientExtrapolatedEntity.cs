using Godot;
using MonkeNet.Serializer;

namespace MonkeNet.Client;

/// <summary>
/// Entity not controlled by this client (e.g. a remote player) that is simulated forward to the local tick instead of being
/// rendered in the past like a <see cref="ClientInterpolatedEntity"/>. Since it lives in the same tick as the local player
/// it can physically interact with locally predicted entities (player-player collisions).
/// Extrapolated entities are handled by the <see cref="ClientPredictionManager"/>, so they are saved every tick, checked
/// against incoming snapshots and rolled back / re-simulated together with the local predicted entities.
/// </summary>
[GlobalClass, Icon("res://addons/monke-net/resources/circle_nodes_solid.png")]
public partial class ClientExtrapolatedEntity : ClientPredictedEntity
{
    // Advance the entity one tick using the last known authoritative state (velocity, last input, etc.), called every tick and when re-simulating
    public virtual void OnExtrapolateTick() { }

    // Local inputs don't belong to this entity, so they are never forwarded
    public sealed override void OnProcessTick(int tick, IPackableElement input) => OnExtrapolateTick();
    public sealed override void ResimulateTick(IPackableElement input) => OnExtrapolateTick();
}
