using System.Collections.Generic;
using System.Linq;
using Godot;
using ImGuiNET;
using MemoryPack;
using NetMessage;
using Vector2 = System.Numerics.Vector2;

/*
    Main player script, send movement packets to the server, does CSP, and reconciliation.
*/
public partial class ClientPlayer : CharacterBody3D
{
    private readonly List<LocalInput> _userInputs = new();

    private int _networkId = -1;
    private int _lastStampReceived = 0;
    private int _misspredictionCounter = 0;

    private byte _automoveInput = 0b0000_1000;
    private bool _autoMoveEnabled = false;

    public override void _Ready()
    {
        _networkId = Multiplayer.GetUniqueId();
    }

    public void ProcessTick(int currentTick)
    {
        var userInput = GenerateUserInput(currentTick);
        if (_autoMoveEnabled)
        {
            SolveAutoMove();
            userInput.Input = _automoveInput;
        }
        _userInputs.Add(userInput);
        SendInputs(currentTick);
        AdvancePhysics(userInput.Input);
    }

    // Applies inputs ahead of the server (Prediction)
    private void AdvancePhysics(byte input)
    {
        this.Velocity = PlayerMovement.ComputeMotion(
            this.GetRid(),
            this.GlobalTransform,
            this.Velocity,
            PlayerMovement.InputToDirection(input));

        Position += this.Velocity * PlayerMovement.FrameDelta;
    }

    // Called when a UserState is received from the server
    // Here we validate that our prediction was correct
    public void ReceiveState(EntityState state, int forTick)
    {
        // Ignore any stamp that should have been received in the past
        if (forTick > _lastStampReceived)
            _lastStampReceived = forTick;
        else return;

        // Delete all stored inputs up to that point, we don't need them anymore
        for (int i = _userInputs.Count-1; i >= 0; i--)
            if (_userInputs[i].Tick <= forTick)
                _userInputs.RemoveAt(i);

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
                PlayerMovement.InputToDirection(userInput.Input));

            expectedTransform.Origin += expectedVelocity * PlayerMovement.FrameDelta;
        }

        var deviation = expectedTransform.Origin - Position; // expectedTransform is where we should be, Position is our current position

        // Reconciliation with authoritative state if the deviation is too high
        if (deviation.Length() > 0)
        {
            this.GlobalTransform = expectedTransform;
            this.Velocity = expectedVelocity;
            _misspredictionCounter++;
            GD.PrintErr($"Client {this.Multiplayer.GetUniqueId()} prediction mismatch ({deviation.Length()}) (Stamp {forTick})!\nExpected Pos:{expectedTransform.Origin} Vel:{expectedVelocity}\nCalculated Pos:{Position} Vel:{Velocity}\n");
        }
    }

    // For Debug Only
    private void SolveAutoMove()
    {
        if (this.Position.X > 5 && _automoveInput == 0b0000_1000)
        {
            _automoveInput = 0b0000_0100;
        }
        else if (this.Position.X < -5 && _automoveInput == 0b0000_0100)
        {
            _automoveInput = 0b0000_1000;
        }
    }

    // Sends all non-processed inputs to the server
    private void SendInputs(int currentTick)
    {
        var userCmd = new UserCommand
        {
            Tick = currentTick,
            Inputs = _userInputs.Select(i => i.Input).ToArray()
        };

        if (this.IsMultiplayerAuthority() && Multiplayer.GetUniqueId() != 1)
        {
            byte[] data = MemoryPackSerializer.Serialize<ICommand>(userCmd);

            (Multiplayer as SceneMultiplayer).SendBytes(data, 1,
                MultiplayerPeer.TransferModeEnum.Unreliable, 0);
        }
    }

    private static LocalInput GenerateUserInput(int tick)
    {
        byte keys = 0;

        if (Input.IsActionPressed("right")) keys |= (byte)InputFlags.Right;
        if (Input.IsActionPressed("left")) keys |= (byte)InputFlags.Left;
        if (Input.IsActionPressed("forward")) keys |= (byte)InputFlags.Forward;
        if (Input.IsActionPressed("backward")) keys |= (byte)InputFlags.Backward;
        if (Input.IsActionPressed("space")) keys |= (byte)InputFlags.Space;
        if (Input.IsActionPressed("shift")) keys |= (byte)InputFlags.Shift;

        var userInput = new LocalInput
        {
            Tick = tick,
            Input = keys
        };

        return userInput;
    }

    public void DrawGui()
    {
        ImGui.Text($"Network Id {_networkId}");
        ImGui.Text($"Position {Position.Snapped(Vector3.One * 0.01f)}");
        ImGui.Text($"Redundant Inputs {_userInputs.Count}");
        ImGui.Text($"Last Stamp Rec. {_lastStampReceived}");
        ImGui.Text($"Misspredictions {_misspredictionCounter}");
        ImGui.Checkbox("Automove?", ref _autoMoveEnabled);
    }
}
