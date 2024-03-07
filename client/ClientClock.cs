using Godot;
using System.Collections.Generic;
using MessagePack;
using ImGuiNET;
using System.Net;
using System;

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

    // Current synced server time
    private int _currentTick = 0;
    private int _immediateLatencyMsec = 0;
    private int _averageLatencyInTicks = 0;
    private int _offsetInTicks = 0;
    private int _jitterInTicks = 0;
    private int _lastOffset = 0;

    private readonly List<int> _offsetValues = new();
    private readonly List<int> _latencyValues = new();

    private SceneMultiplayer _multiplayer;

    public override void _Ready()
    {
        _multiplayer = GetTree().GetMultiplayer() as SceneMultiplayer;
        _multiplayer.PeerPacket += OnPacketReceived;
        GetNode<Timer>("Timer").WaitTime = _sampleRateMs / 1000.0f;
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    public override void _PhysicsProcess(double delta)
    {
        AdjustClock(delta);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        if (command is NetMessage.Sync sync)
        {
            SyncReceived(sync);
        }
    }

    public int GetCurrentTick()
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
        int immediateLatencyInTicks = Mathf.RoundToInt(_immediateLatencyMsec / NetworkUtils.FrameTimeInMsec);

        // Time difference between our clock and the server clock accounting for latency
        _offsetInTicks = (sync.ServerTime - _currentTick) + immediateLatencyInTicks;

        _offsetValues.Add(_offsetInTicks);
        _latencyValues.Add(immediateLatencyInTicks);

        if (_offsetValues.Count >= _sampleSize)
        {
            // Calculate average clock offset for the lasts n samples
            _offsetValues.Sort();
            int offsetAverage = CalculateAverage(_offsetValues, _minLatency);

            // Calculate average latency for the lasts n samples
            _latencyValues.Sort();
            _jitterInTicks = _latencyValues[^1] - _latencyValues[0];
            _averageLatencyInTicks = CalculateAverage(_latencyValues, _minLatency);

            EmitSignal(SignalName.LatencyCalculated, _averageLatencyInTicks);
            GD.Print(_averageLatencyInTicks);
            _lastOffset = offsetAverage; // For adjusting the clock

            _offsetValues.Clear();
            _latencyValues.Clear();
        }
    }

    private void AdjustClock(double delta)
    {
        _currentTick += 1 + _lastOffset;
        if (_lastOffset != 0) { GD.Print($"Adjusted local clock by: {_lastOffset} ticks"); }
        _lastOffset = 0;
    }

    private static int CalculateAverage(List<int> samples, int minValue)
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
        ImGui.Text($"Current Tick {GetCurrentTick()}");
        ImGui.Text($"Immediate Latency {_immediateLatencyMsec}ms");
        ImGui.Text($"Average Latency {_averageLatencyInTicks} ticks");
        ImGui.Text($"Latency Jitter {_jitterInTicks} ticks");
        ImGui.End();
    }
}