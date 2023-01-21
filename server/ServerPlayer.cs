using Godot;
using System;

public partial class ServerPlayer : CharacterBody3D
{
    public Vector2 Input { get; set; } = Vector2.Zero;

    public override void _PhysicsProcess(double delta)
    {
        Velocity = PlayerMovement.ComputeMotion(Velocity, Input, IsOnFloor(), false, delta);
        MoveAndSlide();
    }
}
