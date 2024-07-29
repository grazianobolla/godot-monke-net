using Godot;
using ImGuiNET;
using MemoryPack;
using System;

public partial class ServerClock : ServerNetworkNode
{
	[Signal] public delegate void NetworkProcessTickEventHandler(double delta);

	[Export] private int _netTickrate = 30;
	private double _netTickCounter = 0;
	private int _currentTick = 0;

	public override void _Process(double delta)
	{
		DisplayDebugInformation();
		SolveSendNetworkTickEvent(delta);
	}

	public int ProcessTick()
	{
		_currentTick += 1;
		return _currentTick;
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
	protected override void OnCommandReceived(long id, NetMessage.ICommand command)
	{
		if (command is NetMessage.Sync sync)
		{
			sync.ServerTime = _currentTick;
			SendCommandToClient((int)id, sync, NetworkManager.PacketMode.Unreliable, 1);
		}
	}

	private void DisplayDebugInformation()
	{
		ImGui.Begin($"Clock Information");
		ImGui.Text($"Network Tickrate {GetNetworkTickRate()}hz");
		ImGui.Text($"Current Tick {_currentTick}");
		ImGui.End();
	}
}
