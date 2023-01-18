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
    private NetworkClock _netClock;
    private Node _entityArray;

    private int _packetTickDifference = 0;

    public override void _Ready()
    {
        Connect();

        _entityArray = GetNode("/root/Main/EntityArray");
        _snapshotInterpolator = new(_interpBufferLenght);

        _netClock = GetNode<NetworkClock>("NetworkClock");
        _netClock.Initialize(_multiplayer);
    }

    public override void _Process(double delta)
    {
        _snapshotInterpolator.InterpolateStates(_entityArray, _netClock.Ticks);
        DebugInfo(delta);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        if (command is NetMessage.GameSnapshot snapshot)
        {
            _snapshotInterpolator.PushState(snapshot);
            _packetTickDifference = _netClock.Ticks - snapshot.Time;
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
        label.Text += $"\nclk {_netClock.Ticks} diff_last {_packetTickDifference}ms";
        label.Text += $"\nping {_netClock.Latency} diff_avg {_netClock.PacketDelta}";

        if (_snapshotInterpolator.InterpolationFactor > 1)
            label.Modulate = Colors.Red;
    }
}