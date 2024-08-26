using Godot;
using ImGuiNET;

namespace MonkeNet.Shared;

/// <summary>
/// This node calculates and displays some general Network data.
/// </summary>
public partial class NetworkDebug : Node
{
    private int _sentPerSecond = 0,
                    _recPerSecond = 0,
                    _receivedPacketsPerSecond = 0,
                    _sentPacketsPerSecond = 0;

    public INetworkManager NetworkManager { get; set; } // FIXME: dependency injection? this looks out of place

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

    private void OnTimerOut()
    {
        _sentPerSecond = NetworkManager.PopStatistic(INetworkManager.NetworkStatisticEnum.SentBytes);
        _recPerSecond = NetworkManager.PopStatistic(INetworkManager.NetworkStatisticEnum.ReceivedBytes);
        _receivedPacketsPerSecond = NetworkManager.PopStatistic(INetworkManager.NetworkStatisticEnum.ReceivedPackets);
        _sentPacketsPerSecond = NetworkManager.PopStatistic(INetworkManager.NetworkStatisticEnum.SentPackets);
    }

    public void DisplayDebugInformation()
    {
        if (ImGui.CollapsingHeader("General Network per Second"))
        {
            ImGui.Text($"Sent Bytes {_sentPerSecond}");
            ImGui.Text($"Rec. Bytes {_recPerSecond}");
            ImGui.Text($"Packets Sent {_sentPacketsPerSecond}");
            ImGui.Text($"Packets Rec. {_receivedPacketsPerSecond}");
        }
    }
}
