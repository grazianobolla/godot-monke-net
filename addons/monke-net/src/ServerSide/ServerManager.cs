using Godot;
using ImGuiNET;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Server;

public partial class ServerManager : Node
{
    [Signal] public delegate void ServerTickEventHandler(int currentTick);
    [Signal] public delegate void ServerNetworkTickEventHandler(int currentTick);
    [Signal] public delegate void ClientConnectedEventHandler(int clientId);
    [Signal] public delegate void ClientDisconnectedEventHandler(int clientId);
    public delegate void CommandReceivedEventHandler(int clientId, IPackableMessage command); // Using a C# signal here because the Godot signal wouldn't accept NetworkMessages.IPackableMessage
    public event CommandReceivedEventHandler CommandReceived;

    public static ServerManager Instance { get; private set; }

    private INetworkManager _networkManager;
    private ServerNetworkClock _serverClock;
    private int _currentTick = 0;

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
        _currentTick = _serverClock.ProcessTick();
        EmitSignal(SignalName.ServerTick, _currentTick);
    }

    private void OnNetworkProcess(double delta)
    {
        EmitSignal(SignalName.ServerNetworkTick, _currentTick);
    }

    public void Initialize(INetworkManager networkManager, int port)
    {
        _networkManager = networkManager;

        _serverClock = GetNode<ServerNetworkClock>("ServerClock");
        _serverClock.NetworkProcessTick += OnNetworkProcess;

        _networkManager.CreateServer(port);
        _networkManager.ClientConnected += OnClientConnected;
        _networkManager.ClientDisconnected += OnClientDisconnected;
        _networkManager.PacketReceived += OnPacketReceived;

        GD.Print("Initialized Server Manager");
    }

    public void SendCommandToClient(MessageTypeEnum type, int clientId, IPackableMessage command, INetworkManager.PacketModeEnum mode, int channel)
    {
        byte[] bin = MessageSerializer.Serialize(type, command);
        _networkManager.SendBytes(bin, clientId, channel, mode);
    }

    public int GetNetworkId()
    {
        return _networkManager.GetNetworkId();
    }

    // Route received Input package to the correspondant Network ID
    private void OnPacketReceived(long id, byte[] bin)
    {
        var command = MessageSerializer.Deserialize(bin);
        CommandReceived?.Invoke((int)id, command);
    }

    private void OnClientConnected(long clientId)
    {
        EmitSignal(SignalName.ClientConnected, (int)clientId);
        GD.Print($"Client {clientId} connected");
    }

    private void OnClientDisconnected(long clientId)
    {
        EmitSignal(SignalName.ClientDisconnected, (int)clientId);
        GD.Print($"Client {clientId} disconnected");
    }

    private void DisplayDebugInformation()
    {
        ImGui.Begin("Server Information");
        ImGui.Text($"Framerate {Engine.GetFramesPerSecond()}fps");
        ImGui.Text($"Physics Tick {Engine.PhysicsTicksPerSecond}hz");
        ImGui.End();
    }
}
