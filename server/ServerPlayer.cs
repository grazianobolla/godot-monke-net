using System.Collections.Generic;
using Godot;
using ImGuiNET;
using NetMessage;

public partial class ServerPlayer : CharacterBody3D
{
	public int MultiplayerID { get; set; } = 0;
	public int InstantLatency { get; set; } = 0;

	private readonly List<LocalInput> _pendingInputs = new();
	private int _skippedTicks = 0;
	private int _inputQueueSize = 0;

#nullable enable
	private LocalInput? _lastInputProcessed = null;
#nullable disable

	public void ProcessPendingCommands(int currentTick)
	{
		if (TryGetInput(currentTick, out var input))
		{
			AdvancePhysics(input);
			_lastInputProcessed = input;

			for (int i = _pendingInputs.Count - 1; i >= 0; i--)
				if (_pendingInputs[i].Tick <= currentTick)
					_pendingInputs.RemoveAt(i);
			
			_inputQueueSize = _pendingInputs.Count;
		}
		else if (_lastInputProcessed.HasValue)
		{
			AdvancePhysics(_lastInputProcessed.Value);
			_skippedTicks++;
		}
	}

	public void PushCommand(UserCommand command)
	{
		var inputsLength = command.Inputs.Length;
		var offset = inputsLength - 1;

		for (var i = 0; i < inputsLength; i++)
		{
			var tick = command.Tick - offset;

			if (!TryGetInput(tick, out _))
			{
				var newInput = new LocalInput
				{
					Tick = tick,
					Input = command.Inputs[i]
				};
				_pendingInputs.Add(newInput);
			}

			offset--;
		}
	}
	
	private bool TryGetInput(int tick, out LocalInput input)
	{
		foreach (var pendingInput in _pendingInputs)
		{
			if (pendingInput.Tick != tick)
				continue;

			input = pendingInput;
			return true;
		}

		input = default;
		return false;
	}

	private void AdvancePhysics(LocalInput input)
	{
		this.Velocity = PlayerMovement.ComputeMotion(
			this.GetRid(),
			this.GlobalTransform,
			this.Velocity,
			PlayerMovement.InputToDirection(input.Input));

		Position += this.Velocity * PlayerMovement.FrameDelta;
	}


	public EntityState GetCurrentState()
	{
		return new EntityState
		{
			Id = MultiplayerID,
			PosArray = new float[3] { this.Position.X, this.Position.Y, this.Position.Z },
			VelArray = new float[3] { this.Velocity.X, this.Velocity.Y, this.Velocity.Z }
		};
	}

	public void DrawGui()
	{
		if (ImGui.CollapsingHeader($"Server Player: {MultiplayerID}"))
		{
			ImGui.Text($"Instant Latency {InstantLatency}");
			ImGui.Text($"Input Queue Count {_inputQueueSize}");
			ImGui.Text($"Missed Frames {_skippedTicks}");
		}
	}
}
