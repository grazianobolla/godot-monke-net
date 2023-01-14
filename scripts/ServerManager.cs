using Godot;
using System.Collections.Generic;
using System;

// Code executed on the server side only, handles network events
public partial class ServerManager : Node
{
    [Export] private PackedScene _characterScene;
    [Export] private int _port = 9999;

    private NodePath _charArrayPath = "/root/Main/CharacterArray";
    private SceneMultiplayer _sceneMultiplayer = new();
    private Queue<UserCommand> _cmdsQueue = new();

    public override void _Ready()
    {
        Create();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Process everything and return state to players
        while (_cmdsQueue.Count > 0)
        {
            var cmd = _cmdsQueue.Dequeue();
            var character = GetNode<Character>($"{_charArrayPath}/{cmd.Id}");
            var direction = new Vector3(cmd.DirX, 0, cmd.DirY);
            character.Translate(direction * (float)delta * 16);
        }

        foreach (Character character in GetNode(_charArrayPath).GetChildren())
        {
            GameState newState = new GameState
            {
                Id = Int32.Parse(character.Name),
                X = character.Position.x,
                Y = character.Position.y,
                Z = character.Position.z
            };

            _sceneMultiplayer.SendBytes(StructHelper.ToByteArray(newState), 0);
        }
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
        Node characterInstance = _characterScene.Instantiate();
        characterInstance.Name = id.ToString();
        GetNode(_charArrayPath).AddChild(characterInstance);
        GD.Print("Peer ", id, " connected");
    }

    private void OnPeerDisconnected(long id)
    {
        GetNode($"{_charArrayPath}/{id}").QueueFree();
        GD.Print("Peer ", id, " disconnected");
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        UserCommand cmd = StructHelper.ToStructure<UserCommand>(data);
        _cmdsQueue.Enqueue(cmd);
    }
}
