using Godot;
using ImGuiNET;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Client;

/// <summary>
/// Main Client-side node, communicates with the server and other components of the client
/// </summary>
public partial class ClientManager : Node
{
    [Signal] public delegate void ClientTickEventHandler(int currentTick, int currentRemoteTick);
    [Signal] public delegate void LatencyCalculatedEventHandler(int latencyAverageTicks, int jitterAverageTicks);
    [Signal] public delegate void NetworkReadyEventHandler();

    public delegate void CommandReceivedEventHandler(IPackableMessage command); // Using a C# signal here because the Godot signal wouldn't accept NetworkMessages.IPackableMessage
    public event CommandReceivedEventHandler CommandReceived;

    public static ClientManager Instance { get; private set; }

    private INetworkManager _networkManager;
    private SnapshotInterpolator _snapshotInterpolator;
    private ClientNetworkClock _clock;
    private NetworkDebug _networkDebug;
    private ClientEntityManager _entityManager;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    public override void _PhysicsProcess(double delta)
    {
        _clock.ProcessTick();
        int currentTick = _clock.GetCurrentTick();                  // Local tick (ex: 100)
        int currentRemoteTick = _clock.GetCurrentRemoteTick();      // Tick at which a packet will arrive to the server if its sent right now (ex: 108) (there is an 8 tick delay client->server)
        EmitSignal(SignalName.ClientTick, currentTick, currentRemoteTick);
    }

    public void Initialize(INetworkManager networkManager, string address, int port)
    {
        _networkManager = networkManager;

        _networkDebug = GetNode<NetworkDebug>("NetworkDebug");
        _networkDebug.NetworkManager = _networkManager;

        // Stores NetworkClock node instance
        _clock = GetNode<ClientNetworkClock>("ClientClock");
        _clock.LatencyCalculated += OnLatencyCalculated;

        // Stores SnapshotInterpolator node instance
        _snapshotInterpolator = GetNode<SnapshotInterpolator>("SnapshotInterpolator");

        _entityManager = GetNode<ClientEntityManager>("EntityManager");

        _networkManager.CreateClient(address, port);
        _networkManager.PacketReceived += OnPacketReceived;

        GD.Print("Initialized Client Manager");
    }

    public void SendCommandToServer(MessageTypeEnum type, IPackableMessage command, INetworkManager.PacketModeEnum mode, int channel)
    {
        byte[] bin = MessageSerializer.Serialize(type, command);
        _networkManager.SendBytes(bin, 1, channel, mode);
    }

    private void OnPacketReceived(long id, byte[] bin)
    {
        var command = MessageSerializer.Deserialize(bin);
        CommandReceived?.Invoke(command);
    }

    public void MakeEntityRequest(byte entityType) //TODO: This should NOT be here
    {
        _entityManager.MakeEntityRequest(entityType);
    }

    public int GetNetworkId()
    {
        return _networkManager.GetNetworkId();
    }

    private void OnLatencyCalculated(int latencyAverageTicks, int jitterAverageTicks)
    {
        EmitSignal(SignalName.LatencyCalculated, latencyAverageTicks, jitterAverageTicks);
        EmitSignal(SignalName.NetworkReady); //TODO: calculate this in other way, this should only be emmited once and
                                             //right now it will be emitted every time the colck calculates latency
    }

    private void DisplayDebugInformation()
    {
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
        if (ImGui.Begin("Client Information",
                ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"Network ID {Multiplayer.GetUniqueId()}");
            ImGui.Text($"Framerate {Engine.GetFramesPerSecond()}fps");
            ImGui.Text($"Physics Tick {Engine.PhysicsTicksPerSecond}hz");
            _clock.DisplayDebugInformation();
            _networkDebug.DisplayDebugInformation();
            _snapshotInterpolator.DisplayDebugInformation();
            ImGui.End();
        }
    }
}
