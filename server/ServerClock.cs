using Godot;
using MessagePack;
using ImGuiNET;

public partial class ServerClock : Node
{
	[Signal]
	public delegate void NetworkProcessTickEventHandler(double delta);

	private SceneMultiplayer _multiplayer;

	[Export] private int _netTickrate = 30;
	private double _netTickCounter = 0;

	public override void _Ready()
	{
		_multiplayer = GetTree().GetMultiplayer() as SceneMultiplayer;
		_multiplayer.PeerPacket += OnPacketReceived;
	}

	public override void _Process(double delta)
	{

		DisplayDebugInformation();

		_netTickCounter += delta;
		if (_netTickCounter >= (1.0 / _netTickrate))
		{
			EmitSignal(SignalName.NetworkProcessTick, _netTickCounter);
			_netTickCounter = 0;
		}
	}

	public static int GetCurrentTime()
	{
		return (int)Time.GetTicksMsec();
	}

	public int GetNetworkTickRate()
	{
		return _netTickrate;
	}


	// When we receive a sync packet from a Client, we return it with the current Clock data
	private void OnPacketReceived(long id, byte[] data)
	{
		var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

		if (command is NetMessage.Sync sync)
		{
			sync.ServerTime = GetCurrentTime();
			_multiplayer.SendBytes(MessagePackSerializer.Serialize<NetMessage.ICommand>(sync), (int)id, MultiplayerPeer.TransferModeEnum.Unreliable, 1);
		}
	}

	private void DisplayDebugInformation()
	{
		ImGui.Begin($"Clock Information");
		ImGui.Text($"Network Tickrate {GetNetworkTickRate()}hz");
		ImGui.Text($"Physics Tickrate {Engine.PhysicsTicksPerSecond}hz");
		ImGui.Text($"Current Time {GetCurrentTime()}ms");
		ImGui.End();
	}
}
