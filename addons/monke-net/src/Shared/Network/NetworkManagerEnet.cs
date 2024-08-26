using Godot;

namespace MonkeNet.Shared;

/// <summary>
/// Client/Server Network communication via Godot Enet
/// </summary>
public partial class NetworkManagerEnet : Node, INetworkManager
{
    public enum AudienceMode : int
    {
        Broadcast = 0,
        Server = 1
    }

    private int _networkId = 0;
    private SceneMultiplayer _multiplayer;

    public event INetworkManager.ClientConnectedEventHandler ClientConnected;
    public event INetworkManager.ClientDisconnectedEventHandler ClientDisconnected;
    public event INetworkManager.PacketReceivedEventHandler PacketReceived;

    public override void _Ready()
    {
        _multiplayer = Multiplayer as SceneMultiplayer;

        _multiplayer.PeerConnected += OnPeerConnected;
        _multiplayer.PeerDisconnected += OnPeerDisconnected;
        _multiplayer.PeerPacket += OnPacketReceived;
    }

    public void CreateServer(int port, int maxClients = 32)
    {
        ENetMultiplayerPeer enet = new();
        enet.CreateServer(port, maxClients);
        _multiplayer.MultiplayerPeer = enet;
        _networkId = _multiplayer.GetUniqueId();
        GD.Print($"Created server, Port:{port} Max Clients:{maxClients}");
    }

    public void CreateClient(string address, int port)
    {
        ENetMultiplayerPeer enet = new();
        enet.CreateClient(address, port);
        _multiplayer.MultiplayerPeer = enet;
        _networkId = _multiplayer.GetUniqueId();
        GD.Print($"Client {_multiplayer.GetUniqueId()} connected to {address}:{port}");
    }

    public void SendBytes(byte[] bin, int id, int channel, INetworkManager.PacketModeEnum mode)
    {
        MultiplayerPeer.TransferModeEnum m = mode == INetworkManager.PacketModeEnum.Reliable ? MultiplayerPeer.TransferModeEnum.Reliable : MultiplayerPeer.TransferModeEnum.Unreliable;
        _multiplayer.SendBytes(bin, id, m, channel);
    }

    public int PopStatistic(INetworkManager.NetworkStatisticEnum statistic)
    {
        var enetHost = (_multiplayer.MultiplayerPeer as ENetMultiplayerPeer).Host;

        switch (statistic)
        {
            case INetworkManager.NetworkStatisticEnum.SentBytes:
                return (int)enetHost.PopStatistic(ENetConnection.HostStatistic.SentData);
            case INetworkManager.NetworkStatisticEnum.ReceivedBytes:
                return (int)enetHost.PopStatistic(ENetConnection.HostStatistic.ReceivedData);
            case INetworkManager.NetworkStatisticEnum.SentPackets:
                return (int)enetHost.PopStatistic(ENetConnection.HostStatistic.SentPackets);
            case INetworkManager.NetworkStatisticEnum.ReceivedPackets:
                return (int)enetHost.PopStatistic(ENetConnection.HostStatistic.ReceivedPackets);
            case INetworkManager.NetworkStatisticEnum.PacketLoss:
            case INetworkManager.NetworkStatisticEnum.RoundTripTime:
            default:
                GD.PrintErr("Undefined statistic");
                return 0;
        }
    }

    public int GetNetworkId()
    {
        return _networkId;
    }

    private void OnPeerConnected(long id)
    {
        ClientConnected?.Invoke(id);
    }

    private void OnPeerDisconnected(long id)
    {
        ClientDisconnected?.Invoke(id);
    }

    private void OnPacketReceived(long id, byte[] bin)
    {
        PacketReceived?.Invoke(id, bin);
    }
}
