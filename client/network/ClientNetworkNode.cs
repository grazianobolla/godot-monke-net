using Godot;

/*
    This node can be inherited anywhere in your game, and will allow you to receive send commands to the server
*/
namespace Client;
public abstract partial class ClientNetworkNode : Node
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

    protected static int NetworkId
    {
        get { return ClientManager.Instance.GetNetworkId(); }
    }

    protected bool NetworkReady
    {
        get { return _networkReady; }
    }
}