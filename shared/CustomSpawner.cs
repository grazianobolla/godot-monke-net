using System.Collections.Generic;
using Godot;
using ImGuiNET;
using Vector2 = System.Numerics.Vector2;

public partial class CustomSpawner : MultiplayerSpawner
{
    [Export] private PackedScene _playerScene;
    [Export] private PackedScene _serverPlayerScene;
    [Export] private PackedScene _dummyScene;

    private static readonly Dictionary<int, Node3D> _spawnedNodes = new();
    public static ClientPlayer LocalPlayer { get; private set; }

    public override void _Ready()
    {
        Callable customSpawnFunctionCallable = new Callable(this, nameof(CustomSpawnFunction));
        this.SpawnFunction = customSpawnFunctionCallable;

        this.SetMultiplayerAuthority(Multiplayer.GetUniqueId());
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        DrawGui();
    }

    public static Node3D GetSpawnedNode(int id)
    {
        return _spawnedNodes.GetValueOrDefault(id);
    }

    // This method is called on all Clients after someone joins the server,
    private Node CustomSpawnFunction(Variant data)
    {
        // Retrieve the Id of the spawned player & our own Id
        int spawnedPlayerID = (int)data;
        int localID = Multiplayer.GetUniqueId();

        // If our localID == 1, we spawn the Player as a ServerPlayer: we are on the server
        if (localID == 1)
        {
            GD.Print("Spawned server character");
            ServerPlayer player = _serverPlayerScene.Instantiate<ServerPlayer>();
            player.Name = spawnedPlayerID.ToString();
            player.MultiplayerID = spawnedPlayerID;
            _spawnedNodes.Add(spawnedPlayerID, player);
            return player;
        }

        // If our localID equals the spawnedPlayerID we are in the Client and also the spawned player is ours 
        if (localID == spawnedPlayerID)
        {
            GD.Print("Spawned client player");
            ClientPlayer player = _playerScene.Instantiate<ClientPlayer>();
            player.Name = spawnedPlayerID.ToString();
            player.SetMultiplayerAuthority(spawnedPlayerID);
            LocalPlayer = player;
            _spawnedNodes.Add(spawnedPlayerID, player);
            return player;
        }

        // Id our localId != from spawnedPlayerId and also localId != 1 that means we are in the Client but the spawned player is not ours
        // is from someone else: spawn dummy player
        {
            GD.Print("Spawned dummy");
            Node3D player = _dummyScene.Instantiate<Node3D>();
            player.Name = spawnedPlayerID.ToString();
            player.SetMultiplayerAuthority(spawnedPlayerID);
            _spawnedNodes.Add(spawnedPlayerID, player);
            return player;
        }
    }

    private static void DrawGui()
    {
        if (_spawnedNodes.Count == 0)
            return;
        
        var io = ImGui.GetIO();
        ImGui.SetNextWindowPos(io.DisplaySize with { Y= 0}, ImGuiCond.Always, new Vector2(1, 0));
        if (ImGui.Begin("Player Information",
                ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.AlwaysAutoResize))
        {
            foreach (var nodesValue in _spawnedNodes.Values)
            {
                switch (nodesValue)
                {
                    case ServerPlayer serverPlayer:
                        serverPlayer.DrawGui();
                        break;
                    case ClientPlayer clientPlayer:
                        clientPlayer.DrawGui();
                        break;
                }
            }
        }
        ImGui.End();
    }
}