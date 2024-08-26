using Godot;
using MonkeNet.Client;
using MonkeNet.Shared;

namespace GameDemo;

public partial class MainScene : Node3D
{
    // When the client clicks "Spawn" we request the server to spawn a Player entity for us
    private void OnSpawnButtonPressed()
    {
        ClientManager.Instance.MakeEntityRequest((byte)GameEntitySpawner.EntityType.Player);
        GetNode("Menu/SpawnButton").QueueFree();
    }

    // Creates game server
    private void OnHostButtonPressed()
    {
        MonkeNetManager.Instance.CreateServer(9999);
        GetNode("Menu").QueueFree();
    }

    // Creates Client and connects to 
    private void OnConnectButtonPressed()
    {
        MonkeNetManager.Instance.CreateClient("localhost", 9999);
        GetNode("Menu/HostButton").QueueFree();
        GetNode("Menu/ConnectButton").QueueFree();
    }
}
