using Godot;
using ImGuiNET;
using NetMessage;
using System.Collections.Generic;
using System.Linq;

namespace Server;

/*
    Main server player movement script, received and applies inputs from the client. 
*/
public partial class ServerPlayerMovement : ServerNetworkNode
{
	private ServerPlayer _player;

	private Dictionary<int, NetMessage.UserInput> _pendingInputs = new();
	private int _skippedTicks = 0;
	private int _inputQueueSize = 0;

#nullable enable
	private NetMessage.UserInput? _lastInputProcessed = null;
#nullable disable

	public override void _Ready()
	{
		base._Ready();
		_player = GetParent<ServerPlayer>();
	}

	public override void _Process(double delta)
	{
		DisplayDebugInformation();
	}

	protected override void OnProcessTick(int currentTick)
	{
		ProcessPendingCommands(currentTick);
	}

	protected override void OnCommandReceived(long peerId, ICommand command)
	{
		if (peerId == _player.PlayerId && command is NetMessage.UserCommand userInput)
		{
			PushCommand(userInput);
		}
	}

	private void ProcessPendingCommands(int currentTick)
	{
		if (_pendingInputs.TryGetValue(currentTick, out NetMessage.UserInput input))
		{
			AdvancePhysics(input);
			_lastInputProcessed = input;

			_pendingInputs = _pendingInputs.Where(pair => pair.Key > currentTick)
			.ToDictionary(pair => pair.Key, pair => pair.Value);
			/* TODO: Using dictionaries for this is probably the worst and most unefficient
				way of queueing non-duplicated inputs, this must be changed in the future. */

			_inputQueueSize = _pendingInputs.Count;
		}
		else if (_lastInputProcessed.HasValue)
		{
			AdvancePhysics((NetMessage.UserInput)_lastInputProcessed);
			_skippedTicks++;
		}
	}

	private void PushCommand(NetMessage.UserCommand command)
	{
		int offset = command.Inputs.Length - 1;

		foreach (var input in command.Inputs)
		{
			int tick = command.Tick - offset;

			if (!_pendingInputs.ContainsKey(tick))
			{
				_pendingInputs.Add(tick, input);
			}

			offset--;
		}
	}

	private void AdvancePhysics(NetMessage.UserInput input)
	{
		_player.Velocity = MovementCalculator.ComputeVelocity(_player, input);
		_player.LateralLookAngle = input.LateralLookAngle;
		_player.MoveAndSlide();
	}

	public NetMessage.EntityState GetCurrentState()
	{
		return new NetMessage.EntityState
		{
			Id = _player.PlayerId,
			PosArray = [_player.Position.X, _player.Position.Y, _player.Position.Z],
			VelArray = [_player.Velocity.X, _player.Velocity.Y, _player.Velocity.Z],
			LateralLookAngle = _player.LateralLookAngle
		};
	}

	private void DisplayDebugInformation()
	{
		ImGui.Begin($"Server Player {_player.PlayerId}");
		ImGui.Text($"Input Queue Count {_inputQueueSize}");
		ImGui.Text($"Missed Frames {_skippedTicks}");
		ImGui.End();
	}
}
