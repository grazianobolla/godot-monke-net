using Godot;
using System;
using MessagePack;

// Code executed on the client side only, handles network events
public partial class ClientManager : Node
{
    [Export] private string _address = "localhost";
    [Export] private int _port = 9999;
    [Export] private int _lerpBufferWindow = 50;
    [Export] private int _maxLerp = 150;

    private SceneMultiplayer _multiplayer = new();
    private SnapshotInterpolator _snapshotInterpolator = new();
    private NetworkClock _netClock;
    private Node _entityArray;

    // Debug only
    private int _packetCounter = 0;
    private int _packetsPerSecond = 0;

    public override void _Ready()
    {
        Connect();

        _entityArray = GetNode("/root/Main/EntityArray");

        _netClock = GetNode<NetworkClock>("NetworkClock");
        _netClock.Initialize(_multiplayer);
        _netClock.LatencyCalculated += OnLatencyCalculated;
    }

    public override void _Process(double delta)
    {
        _snapshotInterpolator.InterpolateStates(_entityArray, NetworkClock.Clock);
        DebugInfo(delta);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        if (command is NetMessage.GameSnapshot snapshot)
        {
            _snapshotInterpolator.PushState(snapshot);

            foreach (NetMessage.UserState state in snapshot.States)
            {
                if (state.Id == Multiplayer.GetUniqueId())
                {
                    CustomSpawner.LocalPlayer.ReceiveState(state);
                }
            }
        }

        _packetCounter += 1;
    }

    private void OnLatencyCalculated(int latencyAverage, int offsetAverage, int jitter)
    {
        _snapshotInterpolator.BufferTime = Mathf.Clamp(latencyAverage + _lerpBufferWindow, 0, _maxLerp);
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
        label.Text += $" len {_snapshotInterpolator.BufferTime}ms \nclk {NetworkClock.Clock} ofst {_netClock.Offset}ms";
        label.Text += $"\nping {_netClock.Latency}ms pps {_packetsPerSecond} jit {_netClock.Jitter}";
        label.Text += $"\nred {CustomSpawner.LocalPlayer.RedundantPackets}";

        if (_snapshotInterpolator.InterpolationFactor > 1)
            label.Modulate = Colors.Red;
    }

    private void OnDebugTimerOut()
    {
        _packetsPerSecond = _packetCounter;
        _packetCounter = 0;
    }
}