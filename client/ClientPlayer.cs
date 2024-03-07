using Godot;
using System.Collections.Generic;
using MessagePack;
using NetMessage;
using ImGuiNET;

/*
    Main player script, send movement packets to the server, does CSP, and reconciliation. 
*/
public partial class ClientPlayer : CharacterBody3D
{
    private readonly List<NetMessage.UserInput> _userInputs = new();

    private int _networkId = -1;
    private int _lastStampReceived = 0;

    public override void _Ready()
    {
        _networkId = Multiplayer.GetUniqueId();
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    public void ProcessTick(int tick)
    {
        var userInput = GenerateUserInput(tick);
        _userInputs.Add(userInput);
        SendInputs();
        MoveLocally(userInput);
    }

    // Applies inputs ahead of the server (Prediction)
    private void MoveLocally(NetMessage.UserInput userInput)
    {
        this.Velocity = PlayerMovement.ComputeMotion(
            this.GetRid(),
            this.GlobalTransform,
            this.Velocity,
            PlayerMovement.InputToDirection(userInput.Keys));

        Position += this.Velocity * PlayerMovement.FrameDelta;
    }

    // Called when a UserState is received from the server
    // Here we validate that our prediction was correct
    public void ReceiveState(NetMessage.UserState state)
    {
        // Ignore any stamp that should have been received in the past
        if (state.Stamp > _lastStampReceived)
            _lastStampReceived = state.Stamp;
        else return;

        // GD.Print(state.Stamp);

        _userInputs.RemoveAll(input => input.Stamp <= state.Stamp); // Delete all stored inputs up to that point, we don't need them anymore

        // Re-apply all inputs that haven't been processed by the server starting from the last acked state (the one just received)
        Transform3D expectedTransform = this.GlobalTransform;
        expectedTransform.Origin = state.Position;

        Vector3 expectedVelocity = state.Velocity;

        foreach (var userInput in _userInputs) // Re-apply all inputs
        {
            expectedVelocity = PlayerMovement.ComputeMotion(
                this.GetRid(),
                expectedTransform,
                expectedVelocity,
                PlayerMovement.InputToDirection(userInput.Keys));

            expectedTransform.Origin += expectedVelocity * PlayerMovement.FrameDelta;
        }

        var deviation = expectedTransform.Origin - Position; // expectedTransform is where we should be, Position is our current position

        // Reconciliation with authoritative state if the deviation is too high
        if (deviation.Length() > 0)
        {
            this.GlobalTransform = expectedTransform;
            this.Velocity = expectedVelocity;

            GD.PrintErr($"Client {this.Multiplayer.GetUniqueId()} prediction mismatch ({deviation.Length()}) (Stamp {state.Stamp})!\nExpected Pos:{expectedTransform.Origin} Vel:{expectedVelocity}\nCalculated Pos:{Position} Vel:{Velocity}\n");
        }
    }

    // Sends all non-processed inputs to the server
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
                MultiplayerPeer.TransferModeEnum.Unreliable, 0);
        }
    }

    private NetMessage.UserInput GenerateUserInput(int tick)
    {
        byte keys = 0;

        if (Input.IsActionPressed("right")) keys |= (byte)InputFlags.Right;
        if (Input.IsActionPressed("left")) keys |= (byte)InputFlags.Left;
        if (Input.IsActionPressed("forward")) keys |= (byte)InputFlags.Forward;
        if (Input.IsActionPressed("backward")) keys |= (byte)InputFlags.Backward;
        if (Input.IsActionPressed("space")) keys |= (byte)InputFlags.Space;
        if (Input.IsActionPressed("shift")) keys |= (byte)InputFlags.Shift;

        var userInput = new NetMessage.UserInput
        {
            Stamp = tick,
            Keys = keys
        };

        return userInput;
    }

    private void DisplayDebugInformation()
    {
        ImGui.Begin("Player Network Information");
        ImGui.Text($"Network Id {_networkId}");
        ImGui.Text($"Position {Position.Snapped(Vector3.One * 0.01f)}");
        ImGui.Text($"Redundant Inputs {_userInputs.Count}");
        ImGui.Text($"Last Stamp Rec. {_lastStampReceived}");
        ImGui.End();
    }
}
