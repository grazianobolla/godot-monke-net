using Godot;
using MonkeNet.Client;
using MonkeNet.Shared;

namespace GameDemo;

public partial class DummyStateExtrapolation : ClientExtrapolatedEntity
{
    [Export] private float _maxDeviationAllowedSquared = 0.01f;
    [Export] private CharacterBody3D _characterBody;
    [Export] private Node3D _skeleton;

    // Keep moving with the last known authoritative velocity
    public override void OnExtrapolateTick()
    {
        Vector3 velocity = _characterBody.Velocity;

        if (!_characterBody.IsOnFloor())
            velocity.Y -= SharedPlayerMovement.Gravity * PhysicsUtils.DeltaTime;

        _characterBody.Velocity = velocity;
        PhysicsUtils.MoveAndSlide(_characterBody);
    }

    // Our extrapolation diverged from what the remote player actually did, return to authoritative state
    public override void HandleReconciliation(IEntityStateData receivedState)
    {
        EntityStateMessage state = (EntityStateMessage)receivedState;
        _characterBody.Position = state.Position;
        _characterBody.Velocity = state.Velocity;
        _skeleton.Rotation = Vector3.Up * (state.Yaw + Mathf.Pi);
    }

    public override bool HasMisspredicted(int tick, IEntityStateData receivedState, Vector3 savedPosition)
    {
        EntityStateMessage state = (EntityStateMessage)receivedState;
        return (state.Position - savedPosition).LengthSquared() > _maxDeviationAllowedSquared;
    }

    public override Vector3 GetPosition()
    {
        return _characterBody.Position;
    }
}
