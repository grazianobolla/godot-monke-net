using Godot;
using System.Collections.Generic;
using MessagePack;
using NetMessage;

// Wrapper scene spawned by the MultiplayerSpawner
public partial class ClientPlayer : CharacterBody3D
{
    private List<NetMessage.UserInput> _userInputs = new();
    private int _seqStamp = 0;
    private Vector3 _velocity = Vector3.Zero;

    public override void _PhysicsProcess(double delta)
    {
        var userInput = GenerateUserInput();
        _userInputs.Add(userInput);
        SendInputs();
        MoveLocally(userInput);
        _seqStamp++;
    }

    public void ReceiveState(NetMessage.UserState state)
    {
        _userInputs.RemoveAll(input => input.Stamp <= state.Stamp);

        Transform3D virtualTransform = this.GlobalTransform;
        virtualTransform.Origin = state.Position;

        Vector3 resultPosition = state.Position;
        Vector3 velocity = state.Velocity;

        foreach (var userInput in _userInputs)
        {
            velocity = PlayerMovement.ComputeMotion(this.GetRid(), virtualTransform, velocity, PlayerMovement.InputToDirection(userInput.Keys), 1 / 30.0);
            resultPosition += velocity * (1 / 30.0f);
            virtualTransform.Origin = resultPosition;
        }

        var deviation = resultPosition - Position;

        if (deviation.Length() > 0)
        {
            GD.PrintErr($"Client {this.Multiplayer.GetUniqueId()} prediction mismatch!");

            // Reconciliation with authoritative state
            this.Position = resultPosition;
            _velocity = velocity;
        }
    }

    private void SendInputs()
    {
        var userCmd = new NetMessage.UserCommand
        {
            Id = Multiplayer.GetUniqueId(),
            Commands = _userInputs.ToArray()
        };

        if (this.IsMultiplayerAuthority() && Multiplayer.GetUniqueId() != 1)
        {
            byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(userCmd);

            (Multiplayer as SceneMultiplayer).SendBytes(data, 1,
                MultiplayerPeer.TransferModeEnum.UnreliableOrdered, 0);
        }
    }

    private void MoveLocally(NetMessage.UserInput userInput)
    {
        _velocity = PlayerMovement.ComputeMotion(this.GetRid(), this.GlobalTransform, _velocity, PlayerMovement.InputToDirection(userInput.Keys), 1 / 30.0);
        Position += _velocity * (1 / 30.0f);
    }

    private NetMessage.UserInput GenerateUserInput()
    {
        byte keys = 0;

        if (Input.IsActionPressed("right")) keys |= (byte)InputFlags.Right;
        if (Input.IsActionPressed("left")) keys |= (byte)InputFlags.Left;
        if (Input.IsActionPressed("forward")) keys |= (byte)InputFlags.Forward;
        if (Input.IsActionPressed("backward")) keys |= (byte)InputFlags.Backward;

        var userInput = new NetMessage.UserInput
        {
            Stamp = _seqStamp,
            Keys = keys
        };

        return userInput;
    }
}
