using Godot;
using System;

public partial class CustomSpawner : MultiplayerSpawner
{
    [Export] private PackedScene _playerScene;
    [Export] private PackedScene _serverPlayerScene;
    [Export] private PackedScene _dummyScene;

    public static ClientPlayer LocalPlayer;

    public override void _Ready()
    {
        Callable customSpawnFunctionCallable = new Callable(this, nameof(CustomSpawnFunction));
        this.SpawnFunction = customSpawnFunctionCallable;

        this.SetMultiplayerAuthority(Multiplayer.GetUniqueId());
    }

    private Node CustomSpawnFunction(Variant data)
    {
        int spawnedPlayerID = (int)data;
        int localID = Multiplayer.GetUniqueId();

        // Server character for simulation
        if (localID == 1)
        {
            GD.Print("Spawned server character");
            ServerPlayer player = _serverPlayerScene.Instantiate() as ServerPlayer;
            player.Name = spawnedPlayerID.ToString();
            player.MultiplayerID = spawnedPlayerID;
            return player;
        }

        // Client player
        if (localID == spawnedPlayerID)
        {
            GD.Print("Spawned client player");
            ClientPlayer player = _playerScene.Instantiate() as ClientPlayer;
            player.Name = spawnedPlayerID.ToString();
            player.SetMultiplayerAuthority(spawnedPlayerID);
            LocalPlayer = player;
            return player;
        }

        // Dummy player
        {
            GD.Print("Spawned dummy");
            Node player = _dummyScene.Instantiate();
            player.Name = spawnedPlayerID.ToString();
            player.SetMultiplayerAuthority(spawnedPlayerID);
            return player;
        }
    }
}
