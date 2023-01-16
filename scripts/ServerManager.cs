using Godot;
using System.Collections.Generic;
using System;

// Code executed on the server side only, handles network events
public partial class ServerManager : Node
{
    [Export] private PackedScene _playerScene;
    [Export] private int _port = 9999;

    private SceneMultiplayer _sceneMultiplayer = new();
    private Queue<UserCommand> _cmdsQueue = new();

    public override void _Ready()
    {
        Create();
    }

    public override void _Process(double delta)
    {
        DebugInfo();
    }

    public override void _PhysicsProcess(double delta)
    {
        var playerArray = GetNode("/root/Main/PlayerArray").GetChildren();

        // Process everything
        while (_cmdsQueue.Count > 0)
        {
            var cmd = _cmdsQueue.Dequeue();
            var player = GetNode<Player>($"/root/Main/PlayerArray/{cmd.Id}");
            var direction = new Vector3(cmd.DirX, 0, cmd.DirY);
            player.Translate(direction * (float)delta * 4);
        }

        // Pack and send GameSnapshot
        double currentTime = Time.GetUnixTimeFromSystem();
        var state = new GameSnapshot(currentTime);

        for (int i = 0; i < playerArray.Count; i++)
        {
            var player = playerArray[i] as Player;

            UserState userState = new UserState
            {
                Id = Int32.Parse(player.Name), //TODO: risky
                X = player.Position.x,
                Y = player.Position.y,
                Z = player.Position.z
            };

            state.States[i] = userState;
        }

        _sceneMultiplayer.SendBytes(StructHelper.ToByteArray(state), 0,
            MultiplayerPeer.TransferModeEnum.Unreliable);
    }

    private void Create()
    {
        _sceneMultiplayer.PeerConnected += OnPeerConnected;
        _sceneMultiplayer.PeerDisconnected += OnPeerDisconnected;
        _sceneMultiplayer.PeerPacket += OnPacketReceived;

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        peer.CreateServer(_port);

        _sceneMultiplayer.MultiplayerPeer = peer;
        GetTree().SetMultiplayer(_sceneMultiplayer);

        GD.Print("Server listening on ", _port);
    }

    private void OnPeerConnected(long id)
    {
        Node playerInstance = _playerScene.Instantiate();
        playerInstance.Name = id.ToString();
        GetNode("/root/Main/PlayerArray").AddChild(playerInstance);
        GD.Print("Peer ", id, " connected");
    }

    private void OnPeerDisconnected(long id)
    {
        GetNode($"/root/Main/PlayerArray/{id}").QueueFree();
        GD.Print("Peer ", id, " disconnected");
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        UserCommand cmd = StructHelper.ToStructure<UserCommand>(data);
        _cmdsQueue.Enqueue(cmd);
    }

    private void DebugInfo()
    {
        var label = GetNode<Label>("Debug/Label2");
        label.Text = $"clk {Time.GetUnixTimeFromSystem()}";
    }
}
