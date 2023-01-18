using Godot;
using System.Collections.Generic;
using MessagePack;

// Client side functionaly that calculates latency and other network related stuff
public partial class NetworkPinger : Node
{
    [Signal]
    public delegate void LatencyCalculatedEventHandler(int lastServerTime, int latency, int packetDelta);

    [Export] private int _sampleSize = 5;
    [Export] private float _sampleRateSeconds = 0.5f;
    [Export] private int _minimumLatency = 20;
    [Export] private int _minimumPacketDelta = 20;

    public int Latency { get; private set; } = 0;
    public int PacketDelta { get; private set; } = 0;

    private List<int> _latencyValues = new();
    private List<int> _packetDeltaValues = new();
    private SceneMultiplayer _multiplayer;

    public void Initialize(SceneMultiplayer multiplayer)
    {
        _multiplayer = multiplayer;
        _multiplayer.PeerPacket += OnPacketReceived;

        GetNode<Timer>("Timer").WaitTime = _sampleRateSeconds;
    }

    private void SyncReceived(NetMessage.Sync sync)
    {
        // Latency as the difference between when the packet was sent and when it came back divided by 2
        var currentLatency = ((int)Time.GetTicksMsec() - sync.ClientTime) / 2;

        // Difference in time between the received packet server time and the client clock
        var currentPacketDelta = sync.ServerTime - ClientClock.Ticks;

        _latencyValues.Add(currentLatency);
        _packetDeltaValues.Add(currentPacketDelta);

        if (_latencyValues.Count >= _sampleSize)
        {
            int latencyAvg = ReturnSmoothAverage(_latencyValues, _minimumLatency);
            int packetDeltaAvg = ReturnSmoothAverage(_packetDeltaValues, _minimumPacketDelta);
            _latencyValues.Clear();
            _packetDeltaValues.Clear();

            Latency = latencyAvg;
            PacketDelta = packetDeltaAvg;

            EmitSignal(SignalName.LatencyCalculated, sync.ServerTime, Latency, PacketDelta);
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