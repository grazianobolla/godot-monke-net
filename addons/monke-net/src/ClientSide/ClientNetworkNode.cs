using Godot;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Client;

/// <summary>
/// This node can be inherited anywhere in your game, and will allow you to receive send commands to the server.
/// </summary>
public abstract partial class ClientNetworkNode : Node
{
    protected virtual void OnCommandReceived(IPackableMessage command) { }
    protected virtual void OnLatencyCalculated(int latencyAverageTicks, int jitterAverageTicks) { }
    protected virtual void OnProcessTick(int currentTick, int currentRemoteTick) { }

    private bool _networkReady = false;

    public override void _Ready()
    {
        ClientManager.Instance.ClientTick += OnProcessTick;
        ClientManager.Instance.NetworkReady += OnNetworkReady;
        ClientManager.Instance.CommandReceived += OnCommandReceived;
        ClientManager.Instance.LatencyCalculated += OnLatencyCalculated;
    }

    protected void SendCommandToServer(MessageTypeEnum type, IPackableMessage command, INetworkManager.PacketModeEnum mode, int channel)
    {
        ClientManager.Instance.SendCommandToServer(type, command, mode, channel);
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