using Godot;
using System.Collections.Generic;
using ImGuiNET;
using MemoryPack;
using System.Linq;
using NetMessage;

/*
    Main player movement script, send movement packets to the server, does CSP, and reconciliation. 
*/
public partial class PlayerMovement : NetworkedNode
{
	[Export] private float MaxDeviationAllowedThousands = 0.1f;                     // Allows for some very small deviation when comparing results with the server state (0.1 == 0.0001 units of deviation allowed)
	[Export] private FirstPersonCameraController _firstPersonCameraController;

	private readonly List<LocalInputData> _userInputs = [];
	private CharacterBody3D _player;
	private int _lastStampReceived = 0;
	private int _misspredictionCounter = 0;
	private byte _automoveInput = 0b0001_1001;
	private bool _autoMoveEnabled = false;

	public override void _Ready()
	{
		base._Ready();
		_player = GetParent<CharacterBody3D>();
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

		if (_autoMoveEnabled)
		{
			SolveAutoMove();
			localInputData.Input.Keys = _automoveInput;
		}
		_userInputs.Add(localInputData);
		SendInputs(currentRemoteTick);
		AdvancePhysics(localInputData);
		localInputData.Position = _player.GlobalPosition;
	}

	protected override void OnServerPacketReceived(ICommand command)
	{
		if (command is NetMessage.GameSnapshot snapshot)
		{
			foreach (NetMessage.EntityState state in snapshot.States)
			{
				if (state.Id == NetworkId)
				{
					ProcessServerState(state, snapshot.Tick);
				}
			}
		}
	}

	private void AdvancePhysics(LocalInputData localInputData)
	{
		_player.Velocity = MovementCalculator.ComputeVelocity(_player, localInputData.Input);
		_player.MoveAndSlide();
	}

	// Sends all non-processed inputs to the server
	private void SendInputs(int currentTick)
	{
		var userCmd = new NetMessage.UserCommand
		{
			Tick = currentTick,
			Inputs = _userInputs.Select(i => i.Input).ToArray()
		};

		byte[] bin = MemoryPackSerializer.Serialize<NetMessage.ICommand>(userCmd);
		this.SendBytesToServer(bin, SendMode.UDP, 0);
	}

	private void ProcessServerState(NetMessage.EntityState incomingState, int incomingStateTick)
	{
		if (!NetworkReady)
			return;

		// Ignore any stamp that should have been received in the past
		if (incomingStateTick > _lastStampReceived)
			_lastStampReceived = incomingStateTick;
		else return;

		_userInputs.RemoveAll(input => input.Tick < incomingStateTick); // Delete all stored inputs up to that point, we don't need them anymore
		Vector3 positionForTick = PopSavedPositionForTick(incomingStateTick);

		if (positionForTick == Vector3.Inf)
		{
			GD.PrintErr($"There was no local state saved for tick {incomingStateTick}");
			return;
		}

		var deviation = incomingState.Position - positionForTick;
		float deviationLength = deviation.LengthSquared();

		if (deviationLength > (MaxDeviationAllowedThousands / 1000.0f))
		{
			// Re-apply all inputs that haven't been processed by the server starting from the last acked state (the one just received)
			_player.Position = incomingState.Position;
			_player.Velocity = incomingState.Velocity;

			for (int i = 0; i < _userInputs.Count; i++) // Re-apply all inputs
			{
				var inputData = _userInputs[i];
				_player.Velocity = MovementCalculator.ComputeVelocity(_player, inputData.Input);

				// Applied workaround https://github.com/grazianobolla/godot4-multiplayer-template/issues/8
				// To be honest I have no idea how this math works, but it does!
				_player.Velocity *= (float)this.GetPhysicsProcessDeltaTime() / (float)this.GetProcessDeltaTime();
				_player.MoveAndSlide();
				_player.Velocity /= (float)this.GetPhysicsProcessDeltaTime() / (float)this.GetProcessDeltaTime();

				inputData.Position = _player.GlobalPosition; // Update the state for this input which was wrong since all states after a missprediction are wrong
			}

			GD.PrintErr($"Client {NetworkId} prediction mismatch ({deviationLength}) (Stamp {incomingStateTick})!\nExpected Pos:{incomingState.Position} Vel:{incomingState.Velocity}\nCalculated Pos:{positionForTick}\n");
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
		byte keys = 0;

		if (Input.IsActionPressed("right")) keys |= (byte)NetMessage.InputFlags.Right;
		if (Input.IsActionPressed("left")) keys |= (byte)NetMessage.InputFlags.Left;
		if (Input.IsActionPressed("forward")) keys |= (byte)NetMessage.InputFlags.Forward;
		if (Input.IsActionPressed("backward")) keys |= (byte)NetMessage.InputFlags.Backward;
		if (Input.IsActionPressed("space")) keys |= (byte)NetMessage.InputFlags.Space;
		if (Input.IsActionPressed("shift")) keys |= (byte)NetMessage.InputFlags.Shift;

		var input = new NetMessage.UserInput
		{
			Keys = keys,
			LateralLookAngle = _firstPersonCameraController.GetLateralRotationAngle()
		};

		return new LocalInputData
		{
			Tick = tick,
			Position = Vector3.Inf,
			Input = input
		};
	}

	// For Debug Only
	private void SolveAutoMove()
	{
		_automoveInput = 0b0001_0001;
		_firstPersonCameraController.RotateCameraLateral(2.0f * PhysicsUtils.FrameTime);
	}

	public void DisplayDebugInformation()
	{
		if (ImGui.Begin("Player Data"))
		{
			ImGui.Text($"Position ({_player.GlobalPosition.X:0.00}, {_player.GlobalPosition.Y:0.00}, {_player.GlobalPosition.Z:0.00})");
			ImGui.Text($"Velocity ({_player.Velocity.X:0.00}, {_player.Velocity.Y:0.00}, {_player.Velocity.Z:0.00})");
			ImGui.Text($"Redundant Inputs {_userInputs.Count}");
			ImGui.Text($"Last Stamp Rec. {_lastStampReceived}");
			ImGui.Text($"Misspredictions {_misspredictionCounter}");
			ImGui.Text($"Saved Local States {_userInputs.Count}");
			ImGui.Checkbox("Automove?", ref _autoMoveEnabled);
			ImGui.End();
		}
	}

	private class LocalInputData
	{
		public int Tick;                    // Tick at which the input was taken
		public Vector3 Position;            // Local predicted position at the time
		public NetMessage.UserInput Input;  // Input message sent to the server
	}
}
