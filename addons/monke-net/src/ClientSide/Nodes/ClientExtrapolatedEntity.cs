using Godot;
using MonkeNet.Shared;

namespace MonkeNet.Client;

/// <summary>
/// Entity not controlled by this client (e.g. a remote player) that is simulated forward to the local tick instead of being
/// rendered in the past like a <see cref="ClientInterpolatedEntity"/>. Since it lives in the same tick as the local player
/// it can physically interact with locally predicted entities (player-player collisions).
/// Extrapolated entities never trigger a rollback, every time a snapshot is received the <see cref="ClientPredictionManager"/>
/// resets them to the authoritative state and fast-forwards them to the current local tick.
/// </summary>
[GlobalClass, Icon("res://addons/monke-net/resources/circle_nodes_solid.png")]
public partial class ClientExtrapolatedEntity : ClientNetworkBehaviour
{
    // Advance the entity one tick using the last known authoritative state (velocity, last input, etc.), called every tick and when fast-forwarding
    public virtual void OnExtrapolateTick() { }
    // Return the entity to the authoritative state received from the server
    public virtual void HandleAuthoritativeState(IEntityStateData receivedState) { }
}
