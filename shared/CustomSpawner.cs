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
        int id = (int)data;

        // Server character for simulation
        if (Multiplayer.GetUniqueId() == 1)
        {
            GD.Print("Spawned server character");
            ServerPlayer player = _serverPlayerScene.Instantiate() as ServerPlayer;
            player.Name = id.ToString();
            player.MultiplayerID = id;
            return player;
        }

        // Client player
        if (id == Multiplayer.GetUniqueId())
        {
            GD.Print("Spawned client player");
            Node player = _playerScene.Instantiate();
            player.Name = id.ToString();
            player.SetMultiplayerAuthority(id);
            LocalPlayer = player as ClientPlayer;
            return player;
        }

        // Dummy player
        {
            GD.Print("Spawned dummy");
            Node player = _dummyScene.Instantiate();
            player.Name = id.ToString();
            player.SetMultiplayerAuthority(id);
            return player;
        }
    }
}
