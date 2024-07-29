using Godot;
using System.Collections.Generic;
using ImGuiNET;
using MemoryPack;

/*
    Syncs the clients clock with the servers one, in the process it calculates latency and other debug information.
    This Node should be self contained.
*/
public partial class ClientClock : NetworkedNode
{
    // Called every time latency is calculated
    [Signal] public delegate void LatencyCalculatedEventHandler(int latencyAverageTicks, int jitterAverageTicks);

    [Export] private int _sampleSize = 11;
    [Export] private float _sampleRateMs = 1000;
    [Export] private int _minLatency = 50;
    [Export] private int _fixedTickMargin = 3;

    private int _currentTick = 0;               // Client/Server Synced Tick
    private int _immediateLatencyMsec = 0;      // Latest Calculated Latency in Milliseconds
    private int _averageLatencyInTicks = 0;     // Average Latency in Ticks
    private int _jitterInTicks = 0;             // Latency Jitter in ticks
    private int _averageOffsetInTicks = 0;      // Average Client to Server clock offset in Ticks
    private int _lastOffset = 0;
    private int _minLatencyInTicks = 0;

    private readonly List<int> _offsetValues = new();
    private readonly List<int> _latencyValues = new();

    public override void _Ready()
    {
        base._Ready();
        GetNode<Timer>("Timer").WaitTime = _sampleRateMs / 1000.0f;
        _minLatencyInTicks = PhysicsUtils.MsecToTick(_minLatency);
    }

    protected override void OnCommandReceived(NetMessage.ICommand command)
    {
        if (command is NetMessage.Sync sync)
        {
            SyncReceived(sync);
        }
    }

    public void ProcessTick()
    {
        _currentTick += 1 + _lastOffset;
        _lastOffset = 0;
    }

    public int GetCurrentTick()
    {
        return _currentTick;
    }

    public int GetCurrentRemoteTick()
    {
        return _currentTick + _averageLatencyInTicks + _jitterInTicks + _fixedTickMargin;
    }

    private static int GetLocalTimeMs()
    {
        return (int)Time.GetTicksMsec();
    }

    private void SyncReceived(NetMessage.Sync sync)
    {
        // Latency as the difference between when the packet was sent and when it came back divided by 2
        _immediateLatencyMsec = (GetLocalTimeMs() - sync.ClientTime) / 2;
        int immediateLatencyInTicks = PhysicsUtils.MsecToTick(_immediateLatencyMsec);

        // Time difference between our clock and the server clock accounting for latency
        int _immediateOffsetInTicks = (sync.ServerTime - _currentTick) + immediateLatencyInTicks;

        _offsetValues.Add(_immediateOffsetInTicks);
        _latencyValues.Add(immediateLatencyInTicks);

        if (_offsetValues.Count >= _sampleSize)
        {
            // Calculate average clock offset for the lasts n samples
            _offsetValues.Sort();
            _averageOffsetInTicks = SimpleAverage(_offsetValues);
            _lastOffset = _averageOffsetInTicks; // For adjusting the clock

            // Calculate average latency for the lasts n samples
            _latencyValues.Sort();
            _jitterInTicks = _latencyValues[^1] - _latencyValues[0];
            _averageLatencyInTicks = SmoothAverage(_latencyValues, _minLatencyInTicks);

            EmitSignal(SignalName.LatencyCalculated, _averageLatencyInTicks, _jitterInTicks);

            GD.Print($"At tick {_currentTick}, latency calculations done. Avg. Latency {_averageLatencyInTicks} ticks, Jitter {_jitterInTicks} ticks, Clock Offset {_lastOffset} ticks");

            _offsetValues.Clear();
            _latencyValues.Clear();
        }
    }

    //FIXME: Can be done with samples.Average() I believe but im too lazy to check
    private static int SimpleAverage(List<int> samples)
    {
        if (samples.Count <= 0)
        {
            return 0;
        }

        int count = 0;
        samples.ForEach(s => count += s);
        return count / samples.Count;
    }

    private static int SmoothAverage(List<int> samples, int minValue)
    {
        int sampleSize = samples.Count;
        int middleValue = samples[samples.Count / 2];
        int sampleCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            int value = samples[i];

            // If the value is way too high, we discard that value because its probably just a random occurrance
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

    //Called every _sampleRateMs
    private void OnTimerOut()
    {
        var sync = new NetMessage.Sync
        {
            ClientTime = GetLocalTimeMs(),
            ServerTime = 0
        };

        SendCommandToServer(sync, NetworkManager.PacketMode.Unreliable, 1);
    }

    public void DisplayDebugInformation()
    {
        if (ImGui.CollapsingHeader("Network Clock Information"))
        {
            ImGui.Text($"Synced Tick {GetCurrentRemoteTick()}");
            ImGui.Text($"Local Tick {GetCurrentTick()}");
            ImGui.Text($"Immediate Latency {_immediateLatencyMsec}ms");
            ImGui.Text($"Average Latency {_averageLatencyInTicks} ticks");
            ImGui.Text($"Latency Jitter {_jitterInTicks} ticks");
            ImGui.Text($"Average Offset {_averageOffsetInTicks} ticks");
        }
    }
}