using Godot;
using ImGuiNET;
using MemoryPack;
using System.Linq;

public partial class ServerManager : Node
{
	[Export] private int _port = 9999;

	public static ServerManager Instance { get; private set; }

	private Godot.Collections.Array<Godot.Node> entityArray;
	private ServerClock _serverClock;
	private int _currentTick = 0;

	public override void _EnterTree()
	{
		Instance = this;

		entityArray = GetNode("/root/Main/EntityArray").GetChildren();

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
		_currentTick = _serverClock.ProcessTick();

		foreach (var player in entityArray.OfType<ServerPlayer>())
		{
			player.ProcessPendingCommands(_currentTick);
		}

	}

	private void NetworkProcess(double delta)
	{
		BroadcastSnapshot(_currentTick);
	}

	// Pack and send GameSnapshot with all entities and their information
	private void BroadcastSnapshot(int currentTick)
	{
		var snapshot = new NetMessage.GameSnapshot
		{
			Tick = currentTick,
			States = new NetMessage.EntityState[entityArray.Count]
		};

		for (int i = 0; i < entityArray.Count; i++)
		{
			var player = entityArray[i] as ServerPlayer; //player
			snapshot.States[i] = player.GetCurrentState();
		}

		byte[] bin = MemoryPackSerializer.Serialize<NetMessage.ICommand>(snapshot);
		NetworkManager.Instance.SendBytes(bin, 0, 0, NetworkManager.PacketMode.Unreliable);
	}

	// Route received Input package to the correspondant Network ID
	private void OnPacketReceived(long id, byte[] data)
	{
		var command = MemoryPackSerializer.Deserialize<NetMessage.ICommand>(data);
		if (command is NetMessage.UserCommand userCommand)
		{
			ServerPlayer player = GetNode($"/root/Main/EntityArray/{id}") as ServerPlayer; //FIXME: do not use GetNode here
			player.PushCommand(userCommand);
		}

	}

	private void OnPlayerConnected(long id)
	{
		Node playerInstance = GetNode<MultiplayerSpawner>("/root/Main/MultiplayerSpawner").Spawn(id);
		entityArray = GetNode("/root/Main/EntityArray").GetChildren();
		GD.Print($"Player {id} connected");
	}

	private void OnPlayerDisconnected(long id)
	{
		var player = GetNode($"/root/Main/EntityArray/{id}");
		entityArray.Remove(player);
		player.QueueFree();
		GD.Print($"Player {id} disconnected");
	}

	// Starts the server
	private void StartListening()
	{
		NetworkManager.Instance.CreateServer(_port);
		NetworkManager.Instance.PlayerConnected += OnPlayerConnected;
		NetworkManager.Instance.PlayerDisconnected += OnPlayerDisconnected;
		NetworkManager.Instance.PacketReceived += OnPacketReceived;
	}

	private void DisplayDebugInformation()
	{
		ImGui.Begin("Server Information");
		ImGui.Text($"Framerate {Engine.GetFramesPerSecond()}fps");
		ImGui.Text($"Physics Tick {Engine.PhysicsTicksPerSecond}hz");
		ImGui.End();
	}
}
