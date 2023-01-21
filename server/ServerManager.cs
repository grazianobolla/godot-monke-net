using Godot;
using System.Collections.Generic;
using System;
using MessagePack;

// Code executed on the server side only, handles network events
public partial class ServerManager : Node
{
    [Export] private int _port = 9999;

    private SceneMultiplayer _multiplayer = new();
    private Queue<NetMessage.UserCommand> _cmdsQueue = new();

    public override void _Ready()
    {
        Create();
    }

    public override void _Process(double delta)
    {
        // Process entity commands
        while (_cmdsQueue.Count > 0)
        {
            var cmd = _cmdsQueue.Dequeue();
            var entity = GetNode($"/root/Main/EntityArray/{cmd.Id}");

            if (entity is ServerPlayer player)
            {
                player.Input = cmd.Direction;
            }
        }

        DebugInfo();
    }

    public override void _PhysicsProcess(double delta)
    {
        var entityArray = GetNode("/root/Main/EntityArray").GetChildren();

        // Pack and send GameSnapshot with all entities and their information
        var snapshot = new NetMessage.GameSnapshot
        {
            Time = (int)Time.GetTicksMsec(),
            States = new NetMessage.UserState[entityArray.Count]
        };

        for (int i = 0; i < entityArray.Count; i++)
        {
            var player = entityArray[i] as ServerPlayer; //player

            var userState = new NetMessage.UserState
            {
                Id = Int32.Parse(player.Name), //TODO: risky
                X = player.Position.x,
                Y = player.Position.y,
                Z = player.Position.z
            };

            snapshot.States[i] = userState;
        }

        byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(snapshot);

        _multiplayer.SendBytes(data, 0,
            MultiplayerPeer.TransferModeEnum.Unreliable);
    }

    private void OnPacketReceived(long id, byte[] data)
    {
        var command = MessagePackSerializer.Deserialize<NetMessage.ICommand>(data);

        switch (command)
        {
            case NetMessage.UserCommand userCmd:
                _cmdsQueue.Enqueue(userCmd);
                break;

            case NetMessage.Sync sync:
                sync.ServerTime = (int)Time.GetTicksMsec();
                _multiplayer.SendBytes(MessagePackSerializer.Serialize<NetMessage.ICommand>(sync), (int)id,
                MultiplayerPeer.TransferModeEnum.Unreliable);
                break;
        }
    }

    private void OnPeerConnected(long id)
    {
        Node playerInstance = GetNode<MultiplayerSpawner>("/root/Main/MultiplayerSpawner").Spawn(id);
        GD.Print("Peer ", id, " connected");
    }

    private void OnPeerDisconnected(long id)
    {
        GetNode($"/root/Main/EntityArray/{id}").QueueFree();
        GD.Print("Peer ", id, " disconnected");
    }

    private void Create()
    {
        _multiplayer.PeerConnected += OnPeerConnected;
        _multiplayer.PeerDisconnected += OnPeerDisconnected;
        _multiplayer.PeerPacket += OnPacketReceived;

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        peer.CreateServer(_port);

        _multiplayer.MultiplayerPeer = peer;
        GetTree().SetMultiplayer(_multiplayer);

        GD.Print("Server listening on ", _port);
    }

    private void DebugInfo()
    {
        var label = GetNode<Label>("Debug/Label2");
        label.Text = $"clk {Time.GetTicksMsec()}";
    }
}
