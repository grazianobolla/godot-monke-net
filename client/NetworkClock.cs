using Godot;
using System.Collections.Generic;
using MessagePack;
using ImGuiNET;

/*
    Syncs the clients clock with the servers one, in the process it calculates latency and other debug information.
    This Node should be self contained.
*/
public partial class NetworkClock : Node
{
    [Signal]
    public delegate void LatencyCalculatedEventHandler(int latencyAverage); // Called every time the latency is calculated

    [Export] private int _sampleSize = 11;
    [Export] private float _sampleRateMs = 500;

    // Current synced server time
    public static int Clock { get; private set; } = 0;

    private int _immediateLatency = 0;
    private int _averageLatency = 0;
    private int _offset = 0;
    private int _jitter = 0;
    private int _lastOffset = 0;
    private double _decimalCollector = 0;

    private readonly List<int> _offsetValues = new();
    private readonly List<int> _latencyValues = new();

    private SceneMultiplayer _multiplayer;

    public void Initialize(SceneMultiplayer multiplayer)
    {
        _multiplayer = multiplayer;
        _multiplayer.PeerPacket += OnPacketReceived;

        GetNode<Timer>("Timer").WaitTime = _sampleRateMs / 1000.0f;
    }

    public override void _Process(double delta)
    {
        AdjustClock(delta);
        DisplayDebugInformation();
    }

    private void SyncReceived(NetMessage.Sync sync)
    {
        // Latency as the difference between when the packet was sent and when it came back divided by 2
        _immediateLatency = ((int)Time.GetTicksMsec() - sync.ClientTime) / 2;

        // Time difference between our clock and the server clock accounting for latency
        _offset = (sync.ServerTime - Clock) + _immediateLatency;

        _offsetValues.Add(_offset);
        _latencyValues.Add(_immediateLatency);

        if (_offsetValues.Count >= _sampleSize)
        {
            _offsetValues.Sort();
            _latencyValues.Sort();

            int offsetAverage = ReturnSmoothAverage(_offsetValues, 20);
            _jitter = _latencyValues[_latencyValues.Count - 1] - _latencyValues[0];
            _averageLatency = ReturnSmoothAverage(_latencyValues, 20);

            EmitSignal(SignalName.LatencyCalculated, _averageLatency);

            _lastOffset = offsetAverage; // For adjusting the clock

            _offsetValues.Clear();
            _latencyValues.Clear();
        }
    }

    private void AdjustClock(double delta)
    {
        int msDelta = (int)(delta * 1000.0);

        Clock += msDelta + _lastOffset;

        // Prevent clock drift
        _decimalCollector += (delta * 1000.0) - msDelta;
        if (_decimalCollector >= 1.00)
        {
            Clock += 1;
            _decimalCollector -= 1.0;
        }

        _lastOffset = 0;
    }

    private static int ReturnSmoothAverage(List<int> samples, int minValue)
    {
        int sampleSize = samples.Count;
        int middleValue = samples[samples.Count / 2];
        int sampleCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            int value = samples[i];

            if (value > (2 * middleValue) && value > minValue)
            {
                samples.RemoveAt(i);
                sampleSize--;
            }
            else
            {
                sampleCount += value;
            }
        }

        return sampleCount / samples.Count;
    }

    private void DisplayDebugInformation()
    {
        ImGui.Begin("Network Clock Information");
        ImGui.Text($"Current Clock {Clock}");
        ImGui.Text($"Immediate Latency {_immediateLatency}");
        ImGui.Text($"Average Latency {_averageLatency}");
        ImGui.Text($"Clock Offset {_lastOffset}");
        ImGui.Text($"Jitter {_jitter}");
        ImGui.End();
    }

    private void OnTimerOut()
    {
        var sync = new NetMessage.Sync
        {
            ClientTime = (int)Time.GetTicksMsec(),
            ServerTime = 0
        };

        byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(sync);
        _multiplayer.SendBytes(data, 1, MultiplayerPeer.TransferModeEnum.Unreliable, 1);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        if (command is NetMessage.Sync sync)
        {
            SyncReceived(sync);
        }
    }
}