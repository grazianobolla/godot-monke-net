using Godot;
using System;
using MessagePack;

// Code executed on the client side only, handles network events
public partial class ClientManager : Node
{
    [Export] private string _address = "localhost";
    [Export] private int _port = 9999;
    [Export] private int _interpBufferLenght = 100;

    public int Clock { get; private set; } = 0;

    private SceneMultiplayer _sceneMultiplayer = new();
    private SnapshotInterpolator _snapshotInterpolator;
    private Node _playersArray;

    //TODO: remove -----------------------------
    private int _pkgCounter = 0, _pkgSec = 0, deltaLatency;
    float offset = 0;
    double decColl = 0;
    bool ft = true;
    //------------------------------------------

    public override void _Ready()
    {
        _playersArray = GetNode("/root/Main/PlayerArray");
        _snapshotInterpolator = new(_interpBufferLenght);
        GetNode<NetworkPinger>("NetworkPinger").LatencyCalculated += OnLatencyCalculated;
        Connect();
    }

    public override void _Process(double delta)
    {
        AdjustClock(delta);
        _snapshotInterpolator.InterpolateStates(_playersArray, Clock);
        DebugInfo(delta);
    }

    private void AdjustClock(double delta)
    {
        Clock += (int)(delta * 1000.0) + deltaLatency;
        Clock += (int)offset;

        deltaLatency = 0;

        decColl += (delta * 1000.0) - (int)(delta * 1000.0);
        if (decColl >= 1.00)
        {
            Clock += 1;
            decColl -= 1.0;
        }
    }

    private void OnLatencyCalculated(int lastServerTime, int latency, int delta)
    {
        deltaLatency = delta;

        if (ft)
        {
            Clock = lastServerTime + latency;
            GD.Print("set cloick to ", Clock);
            ft = false;
        }

        GD.Print($"Got latency {latency} delta {delta}");
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        _pkgCounter++;

        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        switch (command)
        {
            case NetMessage.GameSnapshot snapshot:
                _snapshotInterpolator.PushState(snapshot);
                break;

            case NetMessage.Sync sync:
                GetNode<NetworkPinger>("NetworkPinger").SyncReceived(sync, Clock);
                break;
        }
    }

    private void OnPeerConnected(long id) { }

    private void OnConnectedToServer()
    {
        GetNode<Label>("Debug/Label").Text += $"\n{Multiplayer.GetUniqueId()}";
    }

    private void Connect()
    {
        _sceneMultiplayer.PeerConnected += OnPeerConnected;
        _sceneMultiplayer.ConnectedToServer += OnConnectedToServer;
        _sceneMultiplayer.PeerPacket += OnPacketReceived;

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        peer.CreateClient(_address, _port);
        _sceneMultiplayer.MultiplayerPeer = peer;
        GetTree().SetMultiplayer(_sceneMultiplayer);
        GD.Print("Client connected to ", _address, ":", _port);
    }

    private void DebugInfo(double delta)
    {
        if (Input.IsActionPressed("w"))
            offset += (float)delta * 100;
        else if (Input.IsActionPressed("s"))
            offset -= (float)delta * 100;
        else if (Input.IsActionPressed("ui_accept"))
            offset = 0;

        var label = GetNode<Label>("Debug/Label2");
        label.Modulate = Colors.White;
        label.Text = $"buf {_snapshotInterpolator.BufferCount} ";
        label.Text += String.Format("int {0:0.00}", _snapshotInterpolator.InterpolationFactor);
        label.Text += $"\nclk {Clock} ofs {offset}";
        label.Text += $"\npps {_pkgSec} dcol {decColl}";

        if (_snapshotInterpolator.InterpolationFactor > 1)
            label.Modulate = Colors.Red;
    }

    private void OnTimerOut()
    {
        _pkgSec = _pkgCounter;
        _pkgCounter = 0;
        GetNode<NetworkPinger>("NetworkPinger").SendSync(Clock);
    }
}