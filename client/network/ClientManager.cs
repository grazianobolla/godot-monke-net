using System;
using System.Net.NetworkInformation;
using Godot;
using ImGuiNET;
using MemoryPack;

/*
*	Singleton, call using ClientManager.Instance
*/
public partial class ClientManager : Node
{
	[Export] private string _address = "localhost";
	[Export] private int _port = 9999;

	[Signal] public delegate void ClientTickEventHandler(int currentTick, int currentRemoteTick);
	[Signal] public delegate void NetworkReadyEventHandler();

	public delegate void CommandReceivedEventHandler(NetMessage.ICommand command); // Using a C# signal here because the Godot signal wouldn't accept NetMessage.ICommand
	public event CommandReceivedEventHandler CommandReceived;

	public static ClientManager Instance { get; private set; }

	private SnapshotInterpolator _snapshotInterpolator;
	private ClientClock _clock;
	private Node _entityArray;
	private NetworkDebug _networkDebug;

	public override void _EnterTree()
	{
		Instance = this;

		_entityArray = GetNode("/root/Main/EntityArray");
		_networkDebug = GetNode<NetworkDebug>("Debug");

		// Stores NetworkClock node instance
		_clock = GetNode<ClientClock>("ClientClock");
		_clock.LatencyCalculated += OnLatencyCalculated;

		// Stores SnapshotInterpolator node instance
		_snapshotInterpolator = GetNode<SnapshotInterpolator>("SnapshotInterpolator");
		_snapshotInterpolator.SetEntityArray(_entityArray);

		// Connects to the server
		ConnectClient();
	}

	public override void _Process(double delta)
	{
		DisplayDebugInformation();
	}

	public override void _PhysicsProcess(double delta)
	{
		_clock.ProcessTick();
		int currentTick = _clock.GetCurrentTick();                  // Local tick (ex: 100)
		int currentRemoteTick = _clock.GetCurrentRemoteTick();      // Tick at which a packet will arrive to the server if its sent right now (ex: 108) (there is an 8 tick delay client->server)
		EmitSignal(SignalName.ClientTick, currentTick, currentRemoteTick);
	}

	public void SendCommandToServer(NetMessage.ICommand command, NetworkManager.PacketMode mode, int channel)
	{
		byte[] bin = MemoryPackSerializer.Serialize<NetMessage.ICommand>(command);
		NetworkManager.Instance.SendBytes(bin, 1, channel, mode);
	}

	public int GetNetworkId()
	{
		return NetworkManager.Instance.GetNetworkId();
	}

	private void OnLatencyCalculated(int latencyAverageTicks, int jitterAverageTicks)
	{
		_snapshotInterpolator.SetBufferTime(latencyAverageTicks + jitterAverageTicks);
		EmitSignal(SignalName.NetworkReady);
	}

	private void OnPacketReceived(long id, byte[] bin)
	{
		var command = MemoryPackSerializer.Deserialize<NetMessage.ICommand>(bin);
		CommandReceived?.Invoke(command);
	}

	private void ConnectClient()
	{
		NetworkManager.Instance.ConnectToServer(_address, _port);
		NetworkManager.Instance.PacketReceived += OnPacketReceived;
	}

	private void DisplayDebugInformation()
	{
		ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
		if (ImGui.Begin("Client Information",
				ImGuiWindowFlags.NoMove
				| ImGuiWindowFlags.NoResize
				| ImGuiWindowFlags.AlwaysAutoResize))
		{
			ImGui.Text($"Network ID {Multiplayer.GetUniqueId()}");
			ImGui.Text($"Framerate {Engine.GetFramesPerSecond()}fps");
			ImGui.Text($"Physics Tick {Engine.PhysicsTicksPerSecond}hz");
			_clock.DisplayDebugInformation();
			_networkDebug.DisplayDebugInformation();
			_snapshotInterpolator.DisplayDebugInformation();
			ImGui.End();
		}
	}
}
