using Godot;
using System;
using MessagePack;

/*
	Network manager for the client, handles server connection and routes packages.
*/
public partial class ClientManager : Node
{
	[Export] private string _address = "localhost";
	[Export] private int _port = 9999;
	[Export] private int _lerpBufferWindow = 50;
	[Export] private int _maxLerp = 150;

	private SceneMultiplayer _multiplayer = new();
	private SnapshotInterpolator _snapshotInterpolator;
	private NetworkClock _netClock;
	private Node _entityArray;

	public override void _Ready()
	{
		// Connects to the server
		Connect();

		_entityArray = GetNode("/root/Main/EntityArray");

		// Stores NetworkClock node instance
		_netClock = GetNode<NetworkClock>("NetworkClock");
		_netClock.Initialize(_multiplayer);
		_netClock.LatencyCalculated += OnLatencyCalculated;

		// Stores SnapshotInterpolator node instance
		_snapshotInterpolator = GetNode<SnapshotInterpolator>("SnapshotInterpolator");
	}

	public override void _Process(double delta)
	{
		_snapshotInterpolator.InterpolateStates(_entityArray, NetworkClock.Clock);
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

	private void Connect()
	{
		_multiplayer.PeerPacket += OnPacketReceived;

		ENetMultiplayerPeer peer = new();
		peer.CreateClient(_address, _port);
		_multiplayer.MultiplayerPeer = peer;
		GetTree().SetMultiplayer(_multiplayer);
		GD.Print("Client connected to ", _address, ":", _port);
	}
}
