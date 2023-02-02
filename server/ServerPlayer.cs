using Godot;
using System.Collections.Generic;

struct InputData
{
    public byte Input;
    public int Stamp;
}

public partial class ServerPlayer : CharacterBody3D
{
    public int Stamp { get; set; } = 0;

    private List<InputData> _pendingInputs = new();
    private int _lastStampReceived = 0;

    private Vector3 _velocity = Vector3.Zero;

    public void ProcessPendingCommands()
    {
        foreach (var moveCmd in _pendingInputs)
        {
            Move(moveCmd);
        }

        _pendingInputs.Clear();
    }

    public void PushCommand(NetMessage.UserCommand command)
    {
        int firstStamp = command.Stamp - command.Commands.Length + 1;

        for (int i = 0; i < command.Commands.Length; i++)
        {
            byte input = command.Commands[i];
            int stamp = firstStamp + i;

            if (stamp == _lastStampReceived + 1)
            {
                _pendingInputs.Add(new InputData { Stamp = stamp, Input = input });
                _lastStampReceived = stamp;
            }
        }
    }

    private void Move(InputData input)
    {
        Stamp = input.Stamp;
        _velocity = PlayerMovement.ComputeMotion(this, _velocity, PlayerMovement.InputToDirection(input.Input), 1 / 30.0);
        MoveAndCollide(_velocity * (1 / 30.0f));
    }
}
