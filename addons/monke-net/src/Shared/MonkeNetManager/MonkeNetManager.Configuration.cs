using Godot;

namespace MonkeNet.Shared;

public partial class MonkeNetManager : Node
{
    //TODO: make this into an actual configuration singleton
    public NodePath EntitySpawnerNodePath { get; set; } = new NodePath("/root/MainScene/EntitySpawner");
}