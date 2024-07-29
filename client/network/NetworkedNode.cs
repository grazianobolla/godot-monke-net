using Godot;

public abstract partial class NetworkedNode : Node
{
    protected virtual void OnCommandReceived(NetMessage.ICommand command) { }
    protected virtual void OnProcessTick(int currentTick, int currentRemoteTick) { }

    private bool _networkReady = false;

    public override void _Ready()
    {
        ClientManager.Instance.ClientTick += OnProcessTick;
        ClientManager.Instance.NetworkReady += OnNetworkReady;
        ClientManager.Instance.CommandReceived += OnCommandReceived;
    }

    protected static void SendCommandToServer(NetMessage.ICommand command, NetworkManager.PacketMode mode, int channel)
    {
        ClientManager.Instance.SendCommandToServer(command, mode, channel);
    }

    private void OnNetworkReady()
    {
        _networkReady = true;
    }

    protected int NetworkId
    {
        get { return ClientManager.Instance.GetNetworkId(); }
    }

    protected bool NetworkReady
    {
        get { return _networkReady; }
    }
}