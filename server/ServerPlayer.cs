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

    private Queue<InputData> _pendingInputs = new();
    private int _lastStampReceived = 0;

    private Vector3 _velocity = Vector3.Zero;

    //TODO: this should be dynamic, currently the queue will fill at 4 ticks,
    // that's a constant 133ms delay, but will perform ok in bad network conditions
    private int _packetWindow = 4;

    public void ProcessPendingCommands()
    {
        if (_pendingInputs.Count <= 0)
            return;

        while (_pendingInputs.Count > _packetWindow)
        {
            var input = _pendingInputs.Dequeue();
            GD.PrintErr($"Server dropping package {input.Stamp}");
        }

        var moveCmd = _pendingInputs.Dequeue();
        Move(moveCmd);
    }

    public void PushCommand(NetMessage.UserCommand command)
    {
        int firstStamp = command.Stamp - command.Commands.Length + 1;

        for (int i = 0; i < command.Commands.Length; i++)
        {
            var inputData = new InputData
            {
                Stamp = firstStamp + i,
                Input = command.Commands[i]
            };

            if (inputData.Stamp >= _lastStampReceived + 1)
            {
                _pendingInputs.Enqueue(inputData);
                _lastStampReceived = inputData.Stamp;
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
