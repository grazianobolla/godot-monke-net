using Godot;
using MemoryPack;

public abstract partial class NetworkedNode : Node
{
    public enum SendMode
    {
        TCP, UDP
    }

    protected virtual void OnServerPacketReceived(NetMessage.ICommand command) { }
    protected virtual void OnProcessTick(int currentTick, int currentRemoteTick) { }

    private int _networkId;
    private bool _networkReady = false;
    private SceneMultiplayer _multiplayer;

    public override void _Ready()
    {
        _multiplayer = (SceneMultiplayer)this.Multiplayer;
        _multiplayer.PeerPacket += OnPacketReceived;

        ClientManager.Instance.ClientTick += OnProcessTick;
        ClientManager.Instance.NetworkReady += OnNetworkReady;

        _networkId = _multiplayer.GetUniqueId();
    }

    protected void SendBytesToServer(byte[] bin, SendMode mode, int channel)
    {
        MultiplayerPeer.TransferModeEnum m = mode == SendMode.TCP ? MultiplayerPeer.TransferModeEnum.Reliable : MultiplayerPeer.TransferModeEnum.Unreliable;
        _multiplayer.SendBytes(bin, 1, m, channel);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MemoryPackSerializer.Deserialize<NetMessage.ICommand>(data);
        OnServerPacketReceived(command);
    }

    private void OnNetworkReady()
    {
        _networkReady = true;
    }

    protected int NetworkId
    {
        get { return _networkId; }
    }

    protected bool NetworkReady
    {
        get { return _networkReady; }
    }
}