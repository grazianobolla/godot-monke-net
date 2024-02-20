using Godot;
using System;
using MessagePack;
using System.Linq;
using ImGuiNET;

//
public partial class ServerManager : Node
{
	[Export] private int _port = 9999;

	private SceneMultiplayer _multiplayer = new();
	private Godot.Collections.Array<Godot.Node> entityArray;

	public const int NET_TICKRATE = 30; //hz
	private double _netTickCounter = 0;

	public override void _EnterTree()
	{
		StartListening();
	}

	public override void _Process(double delta)
	{
		DisplayDebugInformation();

		_netTickCounter += delta;
		if (_netTickCounter >= (1.0 / NET_TICKRATE))
		{
			NetworkProcess();
			_netTickCounter = 0;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		entityArray = GetNode("/root/Main/EntityArray").GetChildren();
		ProcessPendingPackets();
	}

	// Called every NET_TICKRATE hz
	private void NetworkProcess()
	{
		BroadcastSnapshot();
	}

	// Process corresponding packets for this tick
	private void ProcessPendingPackets()
	{
		foreach (ServerPlayer player in entityArray.Cast<ServerPlayer>())
		{
			player.ProcessPendingCommands();
		}
	}

	// Pack and send GameSnapshot with all entities and their information
	private void BroadcastSnapshot()
	{
		var snapshot = new NetMessage.GameSnapshot
		{
			Time = (int)Time.GetTicksMsec(),
			States = new NetMessage.UserState[entityArray.Count]
		};

		for (int i = 0; i < entityArray.Count; i++)
		{
			var player = entityArray[i] as ServerPlayer; //player
			snapshot.States[i] = player.GetCurrentState();
		}

		byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(snapshot);

		_multiplayer.SendBytes(data, 0,
			MultiplayerPeer.TransferModeEnum.Unreliable, 0);
	}

	// Route received Input package to the correspondant Network ID
	private void OnPacketReceived(long id, byte[] data)
	{
		var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);
		if (command is NetMessage.UserCommand userCommand)
		{
			ServerPlayer player = GetNode($"/root/Main/EntityArray/{userCommand.Id}") as ServerPlayer; //FIXME: do not use GetNode here
			player.PushCommand(userCommand);
		}

	}

	private void OnPeerConnected(long id)
	{
		Node playerInstance = GetNode<MultiplayerSpawner>("/root/Main/MultiplayerSpawner").Spawn(id);
		GD.Print($"Peer {id} connected");
	}

	private void OnPeerDisconnected(long id)
	{
		GetNode($"/root/Main/EntityArray/{id}").QueueFree();
		GD.Print($"Peer {id} disconnected");
	}

	// Starts the server
	private void StartListening()
	{
		_multiplayer.PeerConnected += OnPeerConnected;
		_multiplayer.PeerDisconnected += OnPeerDisconnected;
		_multiplayer.PeerPacket += OnPacketReceived;

		ENetMultiplayerPeer peer = new();
		peer.CreateServer(_port);

		_multiplayer.MultiplayerPeer = peer;
		GetTree().SetMultiplayer(_multiplayer);

		GD.Print("Server listening on ", _port);
	}

	private static void DisplayDebugInformation()
	{
		ImGui.Begin($"Server Information");
		ImGui.Text($"Current Tickrate {NET_TICKRATE}hz");
		ImGui.Text($"Clock {Time.GetTicksMsec()} ticks");
		ImGui.End();
	}
}
