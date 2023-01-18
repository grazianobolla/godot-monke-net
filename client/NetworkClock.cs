using Godot;
using System.Collections.Generic;
using MessagePack;

// Keeps tracks of delays in the network and adjusts a clock to always be in sync with the server
// also calculates latency
public partial class NetworkClock : Node
{
    [Signal]
    public delegate void LatencyCalculatedEventHandler(int latency, int packetDelta);

    [Export] private int _sampleSize = 11;
    [Export] private float _sampleRateSeconds = 0.5f;
    [Export] private int _minimumPacketDelta = 20;

    public int Ticks { get; private set; } = 0;
    public int Latency { get; private set; } = 0;
    public int PacketDelta { get; private set; } = 0;

    private List<int> _packetDeltaValues = new();
    private SceneMultiplayer _multiplayer;
    private bool _firstPing = true; // Used to sync the timer the first time we receive a ping
    private int _lastPacketDelta = 0;
    private double _decimalCollector = 0;

    public void Initialize(SceneMultiplayer multiplayer)
    {
        _multiplayer = multiplayer;
        _multiplayer.PeerPacket += OnPacketReceived;

        GetNode<Timer>("Timer").WaitTime = _sampleRateSeconds;
    }

    public override void _Process(double delta)
    {
        AdjustClock(delta, _lastPacketDelta);
        _lastPacketDelta = 0;
    }

    private void SyncReceived(NetMessage.Sync sync)
    {
        CalculateValues(sync);
    }

    private void CalculateValues(NetMessage.Sync sync)
    {
        // Latency as the difference between when the packet was sent and when it came back divided by 2
        Latency = ((int)Time.GetTicksMsec() - sync.ClientTime) / 2;

        // Difference in time between the received packet server time and the client clock
        var currentPacketDelta = sync.ServerTime - Ticks;

        _packetDeltaValues.Add(currentPacketDelta);

        if (_packetDeltaValues.Count >= _sampleSize)
        {
            int packetDeltaAvg = ReturnSmoothAverage(_packetDeltaValues, _minimumPacketDelta);
            _packetDeltaValues.Clear();
            PacketDelta = _lastPacketDelta = packetDeltaAvg;

            EmitSignal(SignalName.LatencyCalculated, Latency, PacketDelta);
        }
    }

    private void AdjustClock(double delta, int offset)
    {
        int msDelta = (int)(delta * 1000.0);

        Ticks += msDelta + offset;

        // Prevent clock drift
        _decimalCollector += (delta * 1000.0) - msDelta;
        if (_decimalCollector >= 1.00)
        {
            Ticks += 1;
            _decimalCollector -= 1.0;
        }
    }

    private int ReturnSmoothAverage(List<int> samples, int minValue)
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
            ClientTime = (int)Time.GetTicksMsec()
        };

        byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(sync);
        _multiplayer.SendBytes(data, 1, MultiplayerPeer.TransferModeEnum.Unreliable);
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