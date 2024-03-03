using Godot;
using MessagePack;
using ImGuiNET;
using System;

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
		SolveSendNetworkTickEvent(delta);
	}

	public int GetCurrentTime()
	{
		return (int)Time.GetTicksMsec();
	}

	public int GetCurrentTick()
	{
		return Mathf.RoundToInt(Time.GetTicksMsec() / NetworkUtils.FrameTimeInMsec);
	}

	public int GetNetworkTickRate()
	{
		return _netTickrate;
	}

	private void SolveSendNetworkTickEvent(double delta)
	{
		_netTickCounter += delta;
		if (_netTickCounter >= (1.0 / _netTickrate))
		{
			EmitSignal(SignalName.NetworkProcessTick, _netTickCounter);
			_netTickCounter = 0;
		}
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
		ImGui.Text($"Current Time {GetCurrentTick()}");
		ImGui.End();
	}
}
