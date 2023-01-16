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

    public override void _Ready()
    {
        _snapshotInterpolator = new(_interpBufferLenght);
        Connect();
    }

    public override void _Process(double delta)
    {
        _snapshotInterpolator.InterpolateStates(GetNode("/root/Main/PlayerArray"));
        GetNode<Label>("Debug/Label2").Text = $"buff count {_snapshotInterpolator.BufferCount}";
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        //targetNetPos = blablabal;
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
}