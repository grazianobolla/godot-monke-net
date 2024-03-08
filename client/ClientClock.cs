using Godot;
using System.Collections.Generic;
using MessagePack;
using ImGuiNET;
using System.Net;
using System;
using System.Linq;

/*
    Syncs the clients clock with the servers one, in the process it calculates latency and other debug information.
    This Node should be self contained.
*/
public partial class ClientClock : Node
{
    [Signal]
    public delegate void LatencyCalculatedEventHandler(int latencyAverage); // Called every time the latency is calculated

    [Export] private int _sampleSize = 11;
    [Export] private float _sampleRateMs = 500;
    [Export] private int _minLatency = 50;
    [Export] private int _fixedTickMargin = 2;

    private int _currentTick = 0;               // Client/Server Synced Tick
    private int _immediateLatencyMsec = 0;      // Latest Calculated Latency in Milliseconds
    private int _averageLatencyInTicks = 0;     // Average Latency in Ticks
    private int _jitterInTicks = 0;             // Latency Jitter in ticks
    private int _averageOffsetInTicks = 0;      // Average Client to Server clock offset in Ticks
    private int _lastOffset = 0;
    private int _minLatencyInTicks = 0;

    private int _currentTimeMsec = 0;
    private double _decimalCollector = 0;
    private int _lastOffsetInMsec = 0;

    private readonly List<int> _offsetValues = new();
    private readonly List<int> _latencyValues = new();

    private SceneMultiplayer _multiplayer;

    public override void _Ready()
    {
        _multiplayer = GetTree().GetMultiplayer() as SceneMultiplayer;
        _multiplayer.PeerPacket += OnPacketReceived;
        GetNode<Timer>("Timer").WaitTime = _sampleRateMs / 1000.0f;

        _minLatencyInTicks = PhysicsUtils.MsecToTick(_minLatency);
    }

    public override void _Process(double delta)
    {
        AdjustTime(delta);
        DisplayDebugInformation();
    }

    // Called every Physics Tick, same as in the server
    public void ProcessTick()
    {
        _currentTick += 1 + _lastOffset;

        if (_lastOffset != 0)
        {
            GD.Print($"At Tick: {_currentTick - (_lastOffset + 1)} Adjusted local clock by: {_lastOffset} ticks");
        }

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

    public int GetCurrentClock()
    {
        return _currentTimeMsec;
    }

    private void AdjustTime(double delta)
    {
        int deltaInMsec = (int)(delta * 1000.0);

        _currentTimeMsec += deltaInMsec + _lastOffsetInMsec;
        _lastOffsetInMsec = 0;

        // Prevent clock drift
        _decimalCollector += (delta * 1000.0) - deltaInMsec;
        if (_decimalCollector >= 1.00)
        {
            _currentTimeMsec += 1;
            _decimalCollector -= 1.0;
        }
    }


    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        if (command is NetMessage.Sync sync)
        {
            SyncReceived(sync);
        }
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
            _lastOffsetInMsec = PhysicsUtils.TickToMsec(_lastOffset); // For adjusting the msec clock

            // Calculate average latency for the lasts n samples
            _latencyValues.Sort();
            _jitterInTicks = _latencyValues[^1] - _latencyValues[0];
            _averageLatencyInTicks = SmoothAverage(_latencyValues, _minLatencyInTicks);

            EmitSignal(SignalName.LatencyCalculated, _averageLatencyInTicks);

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

        SendSyncPacket(sync);
    }

    private void SendSyncPacket(NetMessage.Sync sync)
    {

        byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(sync);
        _multiplayer.SendBytes(data, 1, MultiplayerPeer.TransferModeEnum.Unreliable, 1);
    }

    private void DisplayDebugInformation()
    {
        ImGui.Begin("Network Clock Information");
        ImGui.Text($"Synced Tick {GetCurrentRemoteTick()}");
        ImGui.Text($"Local Tick {GetCurrentTick()}");
        ImGui.Text($"Clock {GetCurrentClock()}");
        ImGui.Text($"Immediate Latency {_immediateLatencyMsec}ms");
        ImGui.Text($"Average Latency {_averageLatencyInTicks} ticks");
        ImGui.Text($"Average Offset {_averageOffsetInTicks} ticks");
        ImGui.Text($"Latency Jitter {_jitterInTicks} ticks");
        ImGui.End();
    }
}