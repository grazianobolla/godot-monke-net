using Godot;
using System;

// Code executed on the client side only, handles network events
public partial class ClientManager : Node
{
    [Export] private string _address = "localhost";
    [Export] private int _port = 9999;
    [Export] private float _interpBufferLenght = 0.1f;

    private SceneMultiplayer _sceneMultiplayer = new();
    private SnapshotInterpolator _snapshotInterpolator;
    private Node _playersArray;

    private int _pkgCounter = 0, _pkgSec = 0;

    public override void _Ready()
    {
        _playersArray = GetNode("/root/Main/PlayerArray");
        _snapshotInterpolator = new(_interpBufferLenght);
        Connect();
    }

    public override void _Process(double delta)
    {
        _snapshotInterpolator.InterpolateStates(_playersArray);
        DebugInfo();
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        _pkgCounter++;

        var gameSnapshot = StructHelper.ToStructure<GameSnapshot>(data);
        _snapshotInterpolator.PushState(gameSnapshot);
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

    private void DebugInfo()
    {
        var label = GetNode<Label>("Debug/Label2");
        label.Modulate = Colors.White;
        label.Text = $"buf {_snapshotInterpolator.BufferCount} ";
        label.Text += String.Format("int {0:0.00}", _snapshotInterpolator.InterpolationFactor);
        label.Text += $"\nclk {Time.GetUnixTimeFromSystem()}";
        label.Text += $"\npps {_pkgSec}";

        if (_snapshotInterpolator.InterpolationFactor > 1)
            label.Modulate = Colors.Red;
    }

    private void OnTimerOut()
    {
        _pkgSec = _pkgCounter;
        _pkgCounter = 0;
    }
}