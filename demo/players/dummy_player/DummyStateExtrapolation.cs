using Godot;
using MonkeNet.Client;
using MonkeNet.Shared;

namespace GameDemo;

public partial class DummyStateExtrapolation : ClientExtrapolatedEntity
{
    [Export] private CharacterBody3D _characterBody;
    [Export] private Node3D _skeleton;

    private CharacterInputMessage _lastInput; // Last input processed by the server for this player, assumed to be held until we hear otherwise

    // Simulate the remote player as if it kept pressing the same keys
    public override void OnExtrapolateTick()
    {
        _characterBody.Velocity = SharedPlayerMovement.CalculateVelocity(_characterBody, _lastInput);
        PhysicsUtils.MoveAndSlide(_characterBody);
    }

    public override void HandleAuthoritativeState(IEntityStateData receivedState)
    {
        EntityStateMessage state = (EntityStateMessage)receivedState;
        _characterBody.Position = state.Position;
        _characterBody.Velocity = state.Velocity;
        _skeleton.Rotation = Vector3.Up * (state.Yaw + Mathf.Pi);

        _lastInput = new CharacterInputMessage
        {
            Keys = state.InputKeys,
            CameraYaw = state.Yaw
        };
    }
}
