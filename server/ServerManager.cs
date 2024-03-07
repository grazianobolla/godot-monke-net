using Godot;
using MessagePack;
using ImGuiNET;
using System.Linq;

//
public partial class ServerManager : Node
{
	[Export] private int _port = 9999;

	private SceneMultiplayer _multiplayer = new();
	private Godot.Collections.Array<Godot.Node> entityArray;
	private ServerClock _serverClock;

	public override void _EnterTree()
	{
		StartListening();
		_serverClock = GetNode<ServerClock>("ServerClock");
		_serverClock.NetworkProcessTick += NetworkProcess;
	}

	public override void _Process(double delta)
	{
		DisplayDebugInformation();
	}

	public override void _PhysicsProcess(double delta)
	{
		int currentTick = _serverClock.GetCurrentTick();


		foreach (var player in entityArray.OfType<ServerPlayer>())
		{
			player.ProcessPendingCommands();
		}
	}

	private void NetworkProcess(double delta)
	{
		BroadcastSnapshot();
	}

	// Pack and send GameSnapshot with all entities and their information
	private void BroadcastSnapshot()
	{
		var snapshot = new NetMessage.GameSnapshot
		{
			Time = _serverClock.GetCurrentTime(),
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
		entityArray = GetNode("/root/Main/EntityArray").GetChildren();
		GD.Print($"Peer {id} connected");
	}

	private void OnPeerDisconnected(long id)
	{
		var player = GetNode($"/root/Main/EntityArray/{id}");
		entityArray.Remove(player);
		player.QueueFree();
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

	private void DisplayDebugInformation()
	{
	}
}
