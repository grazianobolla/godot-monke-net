using Godot;
using System;

public partial class ServerPlayer : CharacterBody3D
{
    public uint Stamp { get; set; } = 0;

    public void Move(NetMessage.MoveCommand moveCommand)
    {
        Stamp = moveCommand.Stamp;
        Position += PlayerMovement.ComputeMotion(moveCommand.Direction);
    }
}
