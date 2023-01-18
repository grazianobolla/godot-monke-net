using Godot;
using System;
using MessagePack;

// Wrapper scene spawned by the MultiplayerSpawner
public partial class Player : Node3D
{
    public override void _PhysicsProcess(double delta)
    {
        if (!this.IsMultiplayerAuthority() || Multiplayer.GetUniqueId() == 1)
            return;

        Vector2 inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        var cmd = new NetMessage.UserCommand
        {
            Id = Multiplayer.GetUniqueId(),
            DirX = inputDirection.x,
            DirY = inputDirection.y
        };

        byte[] data = MessagePackSerializer.Serialize<NetMessage.ICommand>(cmd);

        (Multiplayer as SceneMultiplayer).SendBytes(data, 1,
            MultiplayerPeer.TransferModeEnum.Unreliable);
    }
}
