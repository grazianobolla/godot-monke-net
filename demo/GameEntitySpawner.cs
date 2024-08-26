using Godot;
using MonkeNet.Client;
using MonkeNet.Shared;

namespace GameDemo;

// Handles entity spawning on both the client and the server
public partial class GameEntitySpawner : EntitySpawner
{
    public enum EntityType : byte
    {
        Player,
        Prop
    }

    // When an entity is spawned, what should be done on the client side?
    protected override Node HandleEntityCreationClientSide(int entityId, byte entityType)
    {
        if (entityType == (byte)EntityType.Player)
        {
            // TODO: use Authority/Owner herem not EntityId, as EntityId will not always be the same as the network id of the client who spawned it
            PackedScene playerScene = entityId == ClientManager.Instance.GetNetworkId() ?
                GD.Load<PackedScene>("res://demo/players/local_player/LocalPlayer.tscn") :
                GD.Load<PackedScene>("res://demo/players/dummy_player/DummyPlayer.tscn");

            return playerScene.Instantiate(); // Spawn player scene
        }

        throw new System.Exception("No Node was returned for Client Entity Creation event");
    }

    // When an entity is spawned, what should be done on the server side?
    protected override Node HandleEntityCreationServerSide(int entityId, byte entityType)
    {
        if (entityType == (byte)EntityType.Player)
        {
            PackedScene playerScene = GD.Load<PackedScene>("res://demo/players/server_player/ServerPlayer.tscn");
            return playerScene.Instantiate(); // Spawn player scene
        }

        throw new System.Exception("No Node was returned for Server Entity Creation event");
    }

}
