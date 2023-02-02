using Godot;
using System.Collections.Generic;

public partial class ServerPlayer : CharacterBody3D
{
    public int Stamp { get; set; } = 0;

    private List<NetMessage.MoveCommand> _pendingCommands = new();
    private int _lastStampReceived = 0;

    private Vector3 _velocity = Vector3.Zero;

    public void ProcessPendingCommands()
    {
        foreach (var moveCmd in _pendingCommands)
        {
            Move(moveCmd);
        }

        _pendingCommands.Clear();
    }

    public void PushCommand(NetMessage.UserCommand command)
    {
        foreach (var moveCmd in command.Commands)
        {
            if (moveCmd.Stamp == _lastStampReceived + 1)
            {
                _pendingCommands.Add(moveCmd);
                _lastStampReceived = moveCmd.Stamp;
            }
        }
    }

    private void Move(NetMessage.MoveCommand moveCommand)
    {
        Stamp = moveCommand.Stamp;
        _velocity = PlayerMovement.ComputeMotion(this, _velocity, moveCommand.Direction, 1 / 30.0);
        MoveAndCollide(_velocity * (1 / 30.0f));
    }
}
