using Godot;
using System.Collections.Generic;
using ImGuiNET;

public partial class ServerPlayer : CharacterBody3D
{
	public int MultiplayerID { get; set; } = 0;
	public int Stamp { get; private set; } = 0;
	public int InstantLatency { get; set; } = 0;

	private readonly Queue<NetMessage.UserInput> _pendingInputs = new();
	private int _lastStampReceived = 0;
	private int _packetWindow = 12; //TODO: this should be dynamic, currently the queue will fill at 8 ticks
	private int _skippedInputs = 0;

	public override void _Process(double delta)
	{
		DisplayDebugInformation();
	}

	public void ProcessPendingCommands()
	{
		if (_pendingInputs.Count <= 0)
			return;

		while (_pendingInputs.Count > _packetWindow)
		{
			_pendingInputs.Dequeue(); //TODO: Hmmm... is this efficient?
			_skippedInputs++;
		}

		var userInput = _pendingInputs.Dequeue();
		Move(userInput);
	}

	public void PushCommand(NetMessage.UserCommand command)
	{
		foreach (NetMessage.UserInput userInput in command.Commands)
		{
			if (userInput.Stamp == _lastStampReceived + 1)
			{
				_pendingInputs.Enqueue(userInput);
				_lastStampReceived = userInput.Stamp;
			}
		}
	}

	private void Move(NetMessage.UserInput userInput)
	{
		Stamp = userInput.Stamp;

		this.Velocity = PlayerMovement.ComputeMotion(
			this.GetRid(),
			this.GlobalTransform,
			this.Velocity,
			PlayerMovement.InputToDirection(userInput.Keys));

		Position += this.Velocity * PlayerMovement.FrameDelta;
	}


	public NetMessage.UserState GetCurrentState()
	{
		return new NetMessage.UserState
		{
			Id = MultiplayerID,
			PosArray = new float[3] { this.Position.X, this.Position.Y, this.Position.Z },
			VelArray = new float[3] { this.Velocity.X, this.Velocity.Y, this.Velocity.Z },
			Stamp = this.Stamp
		};
	}

	private void DisplayDebugInformation()
	{
		ImGui.Begin($"Server Player {MultiplayerID}");
		ImGui.Text($"Instant Latency {InstantLatency}");
		ImGui.Text($"Input Queue Count {_pendingInputs.Count}");
		ImGui.Text($"Input Queue Lag {_pendingInputs.Count * (1.0f / Engine.PhysicsTicksPerSecond) * 1000}ms");
		ImGui.Text($"Last Stamp Rec. {_lastStampReceived}");
		ImGui.Text($"Skipped Inputs {_skippedInputs}");
		ImGui.End();
	}
}
