using Godot;
using ImGuiNET;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;
using System.Collections.Generic;

namespace MonkeNet.Client;

/// <summary>
/// Main player movement script, send movement packets to the server, does CSP, and reconciliation.
/// </summary>
public abstract partial class CharacterControllerClient<TInputMessage, TEntityState>
    : ClientNetworkNode where TInputMessage : IPackableElement where TEntityState : IPackableElement
{
    protected abstract Vector3 CalculateVelocity(CharacterBody3D body, TInputMessage input);
    protected abstract bool CalculateDeviation(TEntityState incomingState, Vector3 savedState);
    protected abstract void HandleReconciliation(CharacterBody3D body, TEntityState incomingState);
    protected abstract TInputMessage CaptureCurrentInput();

    [Export] private CharacterBody3D _characterBody;
    private int _lastStampReceived = 0;
    private int _misspredictionCounter = 0;
    private readonly List<LocalInputData> _userInputs = [];

    public override void _Ready()
    {
        base._Ready();
        _characterBody = GetParent<CharacterBody3D>();
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    protected override void OnProcessTick(int currentTick, int currentRemoteTick)
    {
        if (!NetworkReady)
            return;

        LocalInputData localInputData = GenerateUserInput(currentRemoteTick);
        _userInputs.Add(localInputData);
        SendInputs(currentRemoteTick);
        AdvancePhysics(localInputData);
        localInputData.Position = _characterBody.Position;
    }

    protected override void OnCommandReceived(IPackableMessage command)
    {
        if (command is GameSnapshot snapshot)
        {
            foreach (IEntityStateMessage state in snapshot.States)
            {
                if (state.EntityId == NetworkId)
                {
                    ProcessServerState(state, snapshot.Tick);
                }
            }
        }
    }

    private void AdvancePhysics(LocalInputData localInputData)
    {
        _characterBody.Velocity = CalculateVelocity(_characterBody, localInputData.Input);
        _characterBody.MoveAndSlide();
    }

    /// <summary>
    /// Sends all non-processed inputs marked with <paramref name="currentTick"/> to the server.
    /// </summary>
    /// <param name="currentTick"></param>
    private void SendInputs(int currentTick)
    {
        var userCmd = new CharacterControllerInput
        {
            Tick = currentTick,
            Inputs = new IPackableElement[_userInputs.Count]
        };

        for (int i = 0; i < _userInputs.Count; i++)
        {
            userCmd.Inputs[i] = _userInputs[i].Input;
        }

        SendCommandToServer(MessageTypeEnum.CharacterControllerData, userCmd, INetworkManager.PacketModeEnum.Unreliable, (int)ChannelEnum.CharacterController);
    }

    private void ProcessServerState(IEntityStateMessage incomingState, int incomingStateTick)
    {
        if (!NetworkReady)
            return;

        // Ignore any stamp that should have been received in the past
        if (incomingStateTick > _lastStampReceived)
            _lastStampReceived = incomingStateTick;
        else return;

        _userInputs.RemoveAll(input => input.Tick < incomingStateTick); // Delete all stored inputs up to that point, we don't need them anymore
        Vector3 savedState = PopSavedPositionForTick(incomingStateTick);

        if (savedState == Vector3.Inf)
        {
            // TODO: probably better to throw exceptions
            GD.PrintErr($"There was no local state saved for tick {incomingStateTick}");
            return;
        }

        var incomingStateCasted = (TEntityState)incomingState;
        bool deviation = CalculateDeviation(incomingStateCasted, savedState);

        if (deviation)
        {
            // Re-apply all inputs that haven't been processed by the server starting from the last acked state (the one just received)
            HandleReconciliation(_characterBody, incomingStateCasted);

            for (int i = 0; i < _userInputs.Count; i++) // Re-apply all inputs
            {
                var inputData = _userInputs[i];
                _characterBody.Velocity = CalculateVelocity(_characterBody, inputData.Input);

                // Applied workaround https://github.com/grazianobolla/godot4-multiplayer-template/issues/8
                // To be honest I have no idea how this math works, but it does!
                _characterBody.Velocity *= (float)this.GetPhysicsProcessDeltaTime() / (float)this.GetProcessDeltaTime();
                _characterBody.MoveAndSlide();
                _characterBody.Velocity /= (float)this.GetPhysicsProcessDeltaTime() / (float)this.GetProcessDeltaTime();

                inputData.Position = _characterBody.GlobalPosition; // Update the state for this input which was wrong since all states after a missprediction are wrong
            }

            _misspredictionCounter++;
        }
    }

    private Vector3 PopSavedPositionForTick(int tick)
    {
        for (int i = 0; i < _userInputs.Count; i++)
        {
            if (_userInputs[i].Tick == tick)
            {
                Vector3 position = _userInputs[i].Position;
                _userInputs.RemoveAt(i);
                return position;
            }
        }

        return Vector3.Inf;
    }

    private LocalInputData GenerateUserInput(int tick)
    {
        var input = CaptureCurrentInput();

        return new LocalInputData
        {
            Tick = tick,
            Position = Vector3.Inf,
            Input = input
        };
    }

    public void DisplayDebugInformation()
    {
        if (ImGui.Begin("Player Data"))
        {
            ImGui.Text($"Position ({_characterBody.GlobalPosition.X:0.00}, {_characterBody.GlobalPosition.Y:0.00}, {_characterBody.GlobalPosition.Z:0.00})");
            ImGui.Text($"Velocity ({_characterBody.Velocity.X:0.00}, {_characterBody.Velocity.Y:0.00}, {_characterBody.Velocity.Z:0.00})");
            ImGui.Text($"Redundant Inputs {_userInputs.Count}");
            ImGui.Text($"Last Stamp Rec. {_lastStampReceived}");
            ImGui.Text($"Misspredictions {_misspredictionCounter}");
            ImGui.Text($"Saved Local States {_userInputs.Count}");
            ImGui.End();
        }
    }

    private class LocalInputData
    {
        public int Tick;            // Tick at which the input was taken
        public Vector3 Position;    // Local predicted position at the time
        public TInputMessage Input; // Input message sent to the server
    }
}