using Godot;
using MessagePack;

/*
	Network manager for the client, handles server connection and routes packages.
*/
public partial class ClientManager : Node
{
	[Export] private string _address = "localhost";
	[Export] private int _port = 9999;
	[Export] private int _lerpBufferWindow = 50;
	[Export] private int _maxLerp = 250;

	private SceneMultiplayer _multiplayer = new();
	private SnapshotInterpolator _snapshotInterpolator;
	private ClientClock _clock;
	private Node _entityArray;

	public override void _EnterTree()
	{
		// Connects to the server
		ConnectClient();

		_entityArray = GetNode("/root/Main/EntityArray");

		// Stores NetworkClock node instance
		_clock = GetNode<ClientClock>("ClientClock");
		_clock.LatencyCalculated += OnLatencyCalculated;

		// Stores SnapshotInterpolator node instance
		_snapshotInterpolator = GetNode<SnapshotInterpolator>("SnapshotInterpolator");
	}

	public override void _Process(double delta)
	{
		_snapshotInterpolator.InterpolateStates(_entityArray, _clock.GetCurrentTick());
	}

	private void OnPacketReceived(long id, byte[] data)
	{
		var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

		if (command is NetMessage.GameSnapshot snapshot)
		{
			ProcessSnapshot(snapshot);
		}
	}

	private void ProcessSnapshot(NetMessage.GameSnapshot snapshot)
	{
		_snapshotInterpolator.PushState(snapshot);

		foreach (NetMessage.UserState state in snapshot.States)
		{
			if (state.Id == Multiplayer.GetUniqueId())
			{
				CustomSpawner.LocalPlayer.ReceiveState(state);
			}
		}
	}

	private void OnLatencyCalculated(int latencyAverage)
	{
		_snapshotInterpolator.BufferTime = Mathf.Clamp(latencyAverage + _lerpBufferWindow, 0, _maxLerp);
	}

	private void ConnectClient()
	{
		_multiplayer.PeerPacket += OnPacketReceived;

		ENetMultiplayerPeer peer = new();
		peer.CreateClient(_address, _port);
		_multiplayer.MultiplayerPeer = peer;
		GetTree().SetMultiplayer(_multiplayer);
		GD.Print("Client connected to ", _address, ":", _port);
	}
}
