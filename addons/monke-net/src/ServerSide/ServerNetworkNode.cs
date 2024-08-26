using Godot;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Server;

/// <summary>
/// This node can be inherited anywhere in the project, and will allow you to receive and send commands from/to a client/s
/// </summary>
public abstract partial class ServerNetworkNode : Node
{
    protected virtual void OnCommandReceived(int clientId, IPackableMessage command) { }
    protected virtual void OnProcessTick(int currentTick) { }
    protected virtual void OnNetworkProcessTick(int currentTick) { }
    protected virtual void OnClientConnected(int peerId) { }
    protected virtual void OnClientDisconnected(int peerId) { }

    public override void _Ready()
    {
        ServerManager.Instance.ServerTick += OnProcessTick;
        ServerManager.Instance.ServerNetworkTick += OnNetworkProcessTick;
        ServerManager.Instance.CommandReceived += OnCommandReceived;
        ServerManager.Instance.ClientConnected += OnClientConnected;
        ServerManager.Instance.ClientDisconnected += OnClientDisconnected;
    }

    protected static void SendCommandToClient(MessageTypeEnum type, int peerId, IPackableMessage command, INetworkManager.PacketModeEnum mode, int channel)
    {
        ServerManager.Instance.SendCommandToClient(type, peerId, command, mode, channel);
    }

    protected int NetworkId
    {
        get { return ServerManager.Instance.GetNetworkId(); }
    }
}