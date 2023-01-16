using Godot;
using System.Collections.Generic;
using MessagePack;

// Client side functionaly that calculates latency and other network related stuff
public partial class NetworkPinger : Node
{
    [Signal] public delegate void LatencyCalculatedEventHandler(int lastServerTime, int latency, int delta);

    private int _latency;
    private int _deltaLatency = 0;
    private List<int> _latencyValues = new();

    public void SyncReceived(NetMessage.Sync sync, int atTime)
    {
        var currentLatency = (atTime - sync.ClientTime) / 2;
        _latencyValues.Add(currentLatency);

        if (_latencyValues.Count >= 9)
        {
            int latencyAvg = 0;
            foreach (int latency in _latencyValues)
            {
                latencyAvg += latency;
            }

            latencyAvg /= _latencyValues.Count;
            _deltaLatency = latencyAvg - _latency;
            _latency = latencyAvg;
            _latencyValues.Clear();

            EmitSignal(SignalName.LatencyCalculated, sync.ServerTime, _latency, _deltaLatency);
        }
    }

    public void SendSync(int currentTime)
    {
        var sync = new NetMessage.Sync
        {
            ClientTime = currentTime
        };

        byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(sync);
        (Multiplayer as SceneMultiplayer).SendBytes(data, 1, MultiplayerPeer.TransferModeEnum.Unreliable);
    }
}
