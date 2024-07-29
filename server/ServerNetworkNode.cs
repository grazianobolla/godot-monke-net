using Godot;

/*
    This node can be inherited anywhere in the project, and will allow you to receive and send commands from/to a client/s
*/
public abstract partial class ServerNetworkNode : Node
{
    protected virtual void OnCommandReceived(long peerId, NetMessage.ICommand command) { }
    protected virtual void OnProcessTick(int currentTick) { }

    public override void _Ready()
    {
        ServerManager.Instance.ServerTick += OnProcessTick;
        ServerManager.Instance.CommandReceived += OnCommandReceived;
    }

    protected static void SendCommandToClient(int peerId, NetMessage.ICommand command, NetworkManager.PacketMode mode, int channel)
    {
        ServerManager.Instance.SendCommandToClient(peerId, command, mode, channel);
    }

    protected int NetworkId
    {
        get { return ServerManager.Instance.GetNetworkId(); }
    }
}