using Godot;
using System;

public partial class ServerPlayer : Node3D
{
    [Export] private float _maxSpeed = 4;
    [Export] private float _acceleration = 2;
    [Export] private float _deAcceleration = 0.8f;

    private bool _moving = false;

    public Vector3 Velocity { get; private set; } = Vector3.Zero;

    public override void _Process(double delta)
    {
        Position += Velocity * (float)delta;

        if (!_moving)
            Velocity *= _deAcceleration;
    }

    public void ProcessMovement(Vector3 direction)
    {
        Velocity += direction * _acceleration;
        Velocity = Velocity.LimitLength(_maxSpeed);

        _moving = direction.Length() > 0;
    }
}
