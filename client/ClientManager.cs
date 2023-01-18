using Godot;
using System;
using MessagePack;

// Code executed on the client side only, handles network events
public partial class ClientManager : Node
{
    [Export] private string _address = "localhost";
    [Export] private int _port = 9999;
    [Export] private int _interpBufferLenght = 100;

    private SceneMultiplayer _multiplayer = new();
    private SnapshotInterpolator _snapshotInterpolator;
    private ClientClock _clock = new();
    private NetworkPinger _netPinger;
    private Node _entityArray;

    private bool _firstPing = true;
    private int _packetDelta = 0;
    private int _packetTickDifference = 0;

    public override void _Ready()
    {
        Connect();

        _entityArray = GetNode("/root/Main/EntityArray");
        _snapshotInterpolator = new(_interpBufferLenght);

        _netPinger = GetNode<NetworkPinger>("NetworkPinger");
        _netPinger.Initialize(_multiplayer);
        _netPinger.LatencyCalculated += OnLatencyCalculated;
    }

    public override void _Process(double delta)
    {
        _clock.AdjustClock(delta, _packetDelta);
        _packetDelta = 0;

        _snapshotInterpolator.InterpolateStates(_entityArray, ClientClock.Ticks);
        DebugInfo(delta);
    }

    private void OnLatencyCalculated(int lastServerTicks, int latency, int packetDelta)
    {
        // Sync the timer clock the first time we do a ping
        if (_firstPing)
        {
            _clock.Setup(lastServerTicks, latency);
            _firstPing = false;
            return;
        }

        _packetDelta = packetDelta;
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        switch (command)
        {
            case NetMessage.GameSnapshot snapshot:
                _snapshotInterpolator.PushState(snapshot);
                _packetTickDifference = ClientClock.Ticks - snapshot.Time;
                break;
        }
    }

    private void OnConnectedToServer()
    {
        GetNode<Label>("Debug/Label").Text += $"\n{Multiplayer.GetUniqueId()}";
    }

    private void Connect()
    {
        _multiplayer.ConnectedToServer += OnConnectedToServer;
        _multiplayer.PeerPacket += OnPacketReceived;

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        peer.CreateClient(_address, _port);
        _multiplayer.MultiplayerPeer = peer;
        GetTree().SetMultiplayer(_multiplayer);
        GD.Print("Client connected to ", _address, ":", _port);
    }

    private void DebugInfo(double delta)
    {
        var label = GetNode<Label>("Debug/Label2");
        label.Modulate = Colors.White;
        label.Text = $"buf {_snapshotInterpolator.BufferCount} ";
        label.Text += String.Format("int {0:0.00}", _snapshotInterpolator.InterpolationFactor);
        label.Text += $"\nclk {ClientClock.Ticks} diff {_packetTickDifference}";
        label.Text += $"\nping {_netPinger.Latency} pkg delta {_netPinger.PacketDelta}";

        if (_snapshotInterpolator.InterpolationFactor > 1)
            label.Modulate = Colors.Red;
    }
}