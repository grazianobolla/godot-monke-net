using Godot;
using System.Collections.Generic;
using MessagePack;

// Client side functionaly that calculates latency and other network related stuff
public partial class NetworkPinger : Node
{
    [Signal]
    public delegate void LatencyCalculatedEventHandler(int lastServerTime, int latency, int deltaLatency);

    [Export]
    private int _minimumLatency = 20;

    public int Latency { get; private set; }
    public int DeltaLatency { get; private set; } = 0;

    private List<int> _latencyValues = new();
    private SceneMultiplayer _multiplayer;

    public void Initialize(SceneMultiplayer multiplayer)
    {
        _multiplayer = multiplayer;
        _multiplayer.PeerPacket += OnPacketReceived;
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        if (command is NetMessage.Sync sync)
        {
            SyncReceived(sync);
        }
    }

    private void SyncReceived(NetMessage.Sync sync)
    {
        var currentLatency = ((int)Time.GetTicksMsec() - sync.ClientTime) / 2;
        _latencyValues.Add(currentLatency);

        if (_latencyValues.Count >= 9)
        {
            int latencyAvg = 0;
            _latencyValues.Sort();
            int middleValue = _latencyValues[4];
            var count = _latencyValues.Count;

            for (int i = 0; i < count; i++)
            {
                var value = _latencyValues[i];
                if (value > (middleValue * 2) && value > _minimumLatency)
                {
                    _latencyValues.Remove(value);
                    GD.Print("Removing value ", value, " too far from ", middleValue);
                    count--;
                }
                else
                {
                    latencyAvg += value;
                }
            }


            latencyAvg /= _latencyValues.Count;
            DeltaLatency = Latency - latencyAvg;
            Latency = latencyAvg;

            _latencyValues.Clear();

            EmitSignal(SignalName.LatencyCalculated, sync.ServerTime, Latency, DeltaLatency);
        }
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
}