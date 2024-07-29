using Godot;
using System;

public partial class CustomSpawner : MultiplayerSpawner
{
    [Export] private PackedScene _playerScene;
    [Export] private PackedScene _serverPlayerScene;
    [Export] private PackedScene _dummyScene;

    public static Client.ClientPlayer LocalPlayer;

    public override void _Ready()
    {
        Callable customSpawnFunctionCallable = new Callable(this, nameof(CustomSpawnFunction));
        this.SpawnFunction = customSpawnFunctionCallable;

        this.SetMultiplayerAuthority(Multiplayer.GetUniqueId());
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
            ServerPlayer player = _serverPlayerScene.Instantiate() as ServerPlayer;
            player.Name = spawnedPlayerID.ToString();
            player.PlayerId = spawnedPlayerID;
            return player;
        }

        // If our localID equals the spawnedPlayerID we are in the Client and also the spawned player is ours 
        if (localID == spawnedPlayerID)
        {
            GD.Print("Spawned client player");
            var player = _playerScene.Instantiate() as Client.ClientPlayer;
            player.Name = spawnedPlayerID.ToString();
            player.SetMultiplayerAuthority(spawnedPlayerID);
            LocalPlayer = player;
            return player;
        }

        // Id our localId != from spawnedPlayerId and also localId != 1 that means we are in the Client but the spawned player is not ours
        // is from someone else: spawn dummy player
        {
            GD.Print("Spawned dummy");
            Node player = _dummyScene.Instantiate();
            player.Name = spawnedPlayerID.ToString();
            player.SetMultiplayerAuthority(spawnedPlayerID);
            return player;
        }
    }
}
