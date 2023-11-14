using Godot;
using System.Collections.Generic;
using MessagePack;

// Keeps tracks of delays in the network and adjusts a clock to always be in sync with the server
// also calculates latency
public partial class NetworkClock : Node
{
    [Signal]
    public delegate void LatencyCalculatedEventHandler(int latencyAverage, int offsetAverage, int jitter);

    [Export] private int _sampleSize = 11;
    [Export] private float _sampleRateMs = 500;

    ///Current synced server time
    public static int Clock { get; private set; } = 0;

    public int InmediateLatency { get; private set; } = 0;
    public int Latency { get; private set; } = 0;
    public int Offset { get; private set; } = 0;
    public int Jitter { get; private set; } = 0;

    private readonly List<int> _offsetValues = new();
    private readonly List<int> _latencyValues = new();

    private SceneMultiplayer _multiplayer;
    private int _lastOffset = 0;
    private double _decimalCollector = 0;

    public void Initialize(SceneMultiplayer multiplayer)
    {
        _multiplayer = multiplayer;
        _multiplayer.PeerPacket += OnPacketReceived;

        GetNode<Timer>("Timer").WaitTime = _sampleRateMs / 1000.0f;
    }

    public override void _Process(double delta)
    {
        AdjustClock(delta, _lastOffset);
        _lastOffset = 0;
    }

    private void SyncReceived(NetMessage.Sync sync)
    {
        CalculateValues(sync);
    }

    private void CalculateValues(NetMessage.Sync sync)
    {
        // Latency as the difference between when the packet was sent and when it came back divided by 2
        InmediateLatency = ((int)Time.GetTicksMsec() - sync.ClientTime) / 2;

        // Time difference between our clock and the server clock accounting for latency
        Offset = (sync.ServerTime - Clock) + InmediateLatency;

        _offsetValues.Add(Offset);
        _latencyValues.Add(InmediateLatency);

        if (_offsetValues.Count >= _sampleSize)
        {
            _offsetValues.Sort();
            _latencyValues.Sort();

            int offsetAverage = ReturnSmoothAverage(_offsetValues, 20);
            Jitter = _latencyValues[_latencyValues.Count - 1] - _latencyValues[0];
            Latency = ReturnSmoothAverage(_latencyValues, 20);

            EmitSignal(SignalName.LatencyCalculated, Latency, offsetAverage, Jitter);

            _lastOffset = offsetAverage; // For adjusting the clock

            _offsetValues.Clear();
            _latencyValues.Clear();
        }
    }

    private void AdjustClock(double delta, int offset)
    {
        int msDelta = (int)(delta * 1000.0);

        Clock += msDelta + offset;

        // Prevent clock drift
        _decimalCollector += (delta * 1000.0) - msDelta;
        if (_decimalCollector >= 1.00)
        {
            Clock += 1;
            _decimalCollector -= 1.0;
        }
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