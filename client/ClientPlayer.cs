using Godot;
using System.Collections.Generic;
using NetMessage;
using System;
using ImGuiNET;
using MemoryPack;
using System.Linq;

/*
    Main player script, send movement packets to the server, does CSP, and reconciliation. 
*/
public partial class ClientPlayer : CharacterBody3D
{
    private readonly List<LocalInput> _userInputs = new();

    public bool NetworkReady = false; // True when the client has synced to the server

    private int _networkId = -1;
    private int _lastStampReceived = 0;
    private int _misspredictionCounter = 0;

    private byte _automoveInput = 0b0000_1001;
    private bool _autoMoveEnabled = false;

    public override void _Ready()
    {
        _networkId = Multiplayer.GetUniqueId();
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    public void ProcessTick(int currentRemoteTick)
    {
        var userInput = GenerateUserInput(currentRemoteTick);
        if (_autoMoveEnabled)
        {
            SolveAutoMove();
            userInput.Input = _automoveInput;
        }
        _userInputs.Add(userInput);
        SendInputs(currentRemoteTick);
        AdvancePhysics(userInput.Input);
        userInput.State = GetCurrentState();
    }

    // Applies inputs ahead of the server (Prediction)
    private void AdvancePhysics(byte input)
    {
        this.Velocity = PlayerMovement.ComputeVelocity(
            this.Velocity,
            PlayerMovement.InputToDirection(input));

        this.MoveAndSlide();
    }

    // Called when a UserState is received from the server
    // Here we validate that our prediction was correct
    public void ReceiveState(NetMessage.EntityState incomingState, int incomingStateTick)
    {
        if (!NetworkReady)
            return;

        // Ignore any stamp that should have been received in the past
        if (incomingStateTick > _lastStampReceived)
            _lastStampReceived = incomingStateTick;
        else return;

        _userInputs.RemoveAll(input => input.Tick < incomingStateTick); // Delete all stored inputs up to that point, we don't need them anymore

        bool ok = PopLocalStateForTick(incomingStateTick, out EntityState stateForTick);

        if (!ok || stateForTick == null)
        {
            GD.PrintErr($"There was no local state saved for tick {incomingStateTick}");
            return;
        }

        var deviation = incomingState.Position - stateForTick.Position;
        float deviationLength = deviation.LengthSquared();

        if (deviationLength > 0)
        {
            // Re-apply all inputs that haven't been processed by the server starting from the last acked state (the one just received)
            this.Position = incomingState.Position;
            this.Velocity = incomingState.Velocity;

            foreach (var userInput in _userInputs) // Re-apply all inputs
            {
                this.Velocity = PlayerMovement.ComputeVelocity(
                    this.Velocity,
                    PlayerMovement.InputToDirection(userInput.Input));

                // Applied workaround https://github.com/grazianobolla/godot4-multiplayer-template/issues/8
                // To be honest I have no idea how this math works, but it does!
                this.Velocity *= (float)this.GetPhysicsProcessDeltaTime() / (float)this.GetProcessDeltaTime();
                this.MoveAndSlide();
                this.Velocity /= (float)this.GetPhysicsProcessDeltaTime() / (float)this.GetProcessDeltaTime();

                userInput.State = GetCurrentState(); // Update the state for this input which was wrong since all states after a missprediction are wrong
            }

            GD.PrintErr($"Client {this.Multiplayer.GetUniqueId()} prediction mismatch ({deviationLength}) (Stamp {incomingStateTick})!\nExpected Pos:{incomingState.Position} Vel:{incomingState.Velocity}\nCalculated Pos:{stateForTick.Position} Vel:{stateForTick.Velocity}\n");
            _misspredictionCounter++;
        }
    }

    private bool PopLocalStateForTick(int tick, out EntityState state)
    {
        for (int i = 0; i < _userInputs.Count; i++)
        {
            if (_userInputs[i].Tick != tick)
                continue;

            state = _userInputs[i].State;
            _userInputs.RemoveAt(i);
            return true;
        }

        state = null;
        return false;
    }

    // For Debug Only
    private void SolveAutoMove()
    {
        if (this.Position.X > 0.03f && _automoveInput == 0b0000_1001)
        {
            _automoveInput = 0b0000_0110;
        }
        else if (this.Position.X < -0.03f && _automoveInput == 0b0000_0110)
        {
            _automoveInput = 0b0000_1001;
        }
    }

    // Sends all non-processed inputs to the server
    private void SendInputs(int currentTick)
    {
        var userCmd = new NetMessage.UserCommand
        {
            Tick = currentTick,
            Inputs = _userInputs.Select(i => i.Input).ToArray()
        };

        if (this.IsMultiplayerAuthority() && Multiplayer.GetUniqueId() != 1)
        {
            byte[] data = MemoryPackSerializer.Serialize<NetMessage.ICommand>(userCmd);

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

    private void DisplayDebugInformation()
    {
        ImGui.Begin("Player Network Information");
        ImGui.Text($"Network Id {_networkId}");
        ImGui.Text($"Position {Position.Snapped(Vector3.One * 0.01f)}");
        ImGui.Text($"Redundant Inputs {_userInputs.Count}");
        ImGui.Text($"Last Stamp Rec. {_lastStampReceived}");
        ImGui.Text($"Misspredictions {_misspredictionCounter}");
        ImGui.Checkbox("Automove?", ref _autoMoveEnabled);
        ImGui.End();
    }

    public NetMessage.EntityState GetCurrentState()
    {
        return new NetMessage.EntityState
        {
            Id = _networkId,
            PosArray = new float[3] { this.Position.X, this.Position.Y, this.Position.Z },
            VelArray = new float[3] { this.Velocity.X, this.Velocity.Y, this.Velocity.Z }
        };
    }

    private class LocalInput
    {
        public int Tick;
        public byte Input;
        public EntityState State; // State of the player at Tick
    }
}
