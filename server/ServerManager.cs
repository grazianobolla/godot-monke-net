using Godot;
using ImGuiNET;
using MemoryPack;

namespace Server;

public partial class ServerManager : Node
{
	[Export] private int _port = 9999;

	[Signal] public delegate void ServerTickEventHandler(int currentTick);
	public delegate void CommandReceivedEventHandler(long peerId, NetMessage.ICommand command); // Using a C# signal here because the Godot signal wouldn't accept NetMessage.ICommand
	public event CommandReceivedEventHandler CommandReceived;

	public static ServerManager Instance { get; private set; }

	private Godot.Collections.Array<Godot.Node> entityArray;
	private ServerNetworkClock _serverClock;
	private int _currentTick = 0;

	public override void _EnterTree()
	{
		Instance = this;

		entityArray = GetNode("/root/Main/EntityArray").GetChildren();

		StartListening();
		_serverClock = GetNode<ServerNetworkClock>("ServerClock");
		_serverClock.NetworkProcessTick += OnNetworkProcess;
	}

	public override void _Process(double delta)
	{
		DisplayDebugInformation();
	}

	public override void _PhysicsProcess(double delta)
	{
		_currentTick = _serverClock.ProcessTick();
		EmitSignal(SignalName.ServerTick, _currentTick);
	}

	private void OnNetworkProcess(double delta)
	{
		BroadcastSnapshot(_currentTick);
	}

	public void SendCommandToClient(int peerId, NetMessage.ICommand command, NetworkManager.PacketMode mode, int channel)
	{
		byte[] bin = MemoryPackSerializer.Serialize<NetMessage.ICommand>(command);
		NetworkManager.Instance.SendBytes(bin, peerId, channel, mode);
	}

	public int GetNetworkId()
	{
		return NetworkManager.Instance.GetNetworkId();
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
		CommandReceived?.Invoke(id, command);
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
