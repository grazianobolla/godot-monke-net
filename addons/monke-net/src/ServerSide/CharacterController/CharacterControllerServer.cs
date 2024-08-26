using Godot;
using ImGuiNET;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;
using System.Collections.Generic;
using System.Linq;

namespace MonkeNet.Server;

/// <summary>
/// Main server player movement script, received and applies inputs from the client. 
/// </summary>
public abstract partial class CharacterControllerServer<TInputMessage> : ServerNetworkNode where TInputMessage : IPackableMessage
{
    protected abstract Vector3 CalculateVelocity(CharacterBody3D body, TInputMessage input);

    private CharacterBody3D _player;
    private int _skippedTicks = 0;
    private int _inputQueueSize = 0;
    private Dictionary<int, TInputMessage> _pendingInputs = [];

#nullable enable
    private TInputMessage? _lastInputProcessed = default;
#nullable disable

    public override void _Ready()
    {
        base._Ready();
        _player = GetParent<CharacterBody3D>();
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    protected override void OnProcessTick(int currentTick)
    {
        ProcessPendingCommands(currentTick);
    }

    protected override void OnCommandReceived(int clientId, IPackableMessage command)
    {
        var playerEntity = _player as INetworkedEntity;
        if (clientId == playerEntity.EntityId && command is CharacterControllerInput characterInput)
        {
            PushCommand(characterInput);
        }
    }

    private void ProcessPendingCommands(int currentTick)
    {
        if (_pendingInputs.TryGetValue(currentTick, out TInputMessage input))
        {
            AdvancePhysics(input);
            _lastInputProcessed = input;

            _pendingInputs = _pendingInputs.Where(pair => pair.Key > currentTick)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
            /* TODO: Using dictionaries for this is probably the worst and most unefficient
				way of queueing non-duplicated inputs, this must be changed in the future. */

            _inputQueueSize = _pendingInputs.Count;
        }
        else if (Equals(_lastInputProcessed, default(TInputMessage)))
        {
            AdvancePhysics(_lastInputProcessed);
            _skippedTicks++;
        }
    }

    private void PushCommand(CharacterControllerInput characterInput)
    {
        int offset = characterInput.Inputs.Length - 1;

        foreach (var input in characterInput.Inputs)
        {
            int tick = characterInput.Tick - offset;

            if (!_pendingInputs.ContainsKey(tick))
            {
                _pendingInputs.Add(tick, (TInputMessage)input);
            }

            offset--;
        }
    }

    private void AdvancePhysics(TInputMessage characterInput)
    {
        _player.Velocity = CalculateVelocity(_player, characterInput);
        _player.MoveAndSlide();
    }

    private void DisplayDebugInformation()
    {
        var playerEntity = _player as INetworkedEntity;
        ImGui.Begin($"Server Player {playerEntity.EntityId}");
        ImGui.Text($"Input Queue Count {_inputQueueSize}");
        ImGui.Text($"Missed Frames {_skippedTicks}");
        ImGui.End();
    }
}