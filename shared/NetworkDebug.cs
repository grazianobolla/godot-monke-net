using Godot;
using ImGuiNET;

/*
	This node calculates and displays some general Debug data.
*/
public partial class NetworkDebug : Control
{
	private double _sentPerSecond = 0, _recPerSecond = 0, _receivedPacketsPerSecond = 0, _sentPacketsPerSecond = 0;

	public override void _Ready()
	{
		Timer timer = new();
		AddChild(timer);

		timer.WaitTime = 1;
		timer.OneShot = false;
		timer.Autostart = true;

		timer.Timeout += OnTimerOut;
		timer.Start();
	}

	public override void _Process(double delta)
	{
		DisplayDebugInformation();
	}

	private void OnTimerOut()
	{
		var enetHost = (Multiplayer.MultiplayerPeer as ENetMultiplayerPeer).Host;
		_sentPerSecond = enetHost.PopStatistic(ENetConnection.HostStatistic.SentData);
		_recPerSecond = enetHost.PopStatistic(ENetConnection.HostStatistic.ReceivedData);
		_receivedPacketsPerSecond = enetHost.PopStatistic(ENetConnection.HostStatistic.ReceivedPackets);
		_sentPacketsPerSecond = enetHost.PopStatistic(ENetConnection.HostStatistic.SentPackets);
	}

	private void DisplayDebugInformation()
	{
		ImGui.Begin("General Network per Second");
		ImGui.Text($"Sent Bytes {_sentPerSecond}");
		ImGui.Text($"Rec. Bytes {_recPerSecond}");
		ImGui.Text($"Packets Sent {_sentPacketsPerSecond}");
		ImGui.Text($"Packets Rec. {_receivedPacketsPerSecond}");
		ImGui.End();
	}
}
