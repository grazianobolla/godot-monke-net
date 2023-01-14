using Godot;
using System;

// Wrapper scene spawned by the MultiplayerSpawner
public partial class Character : Node3D
{
    public override void _Ready()
    {
        this.SetMultiplayerAuthority(Int32.Parse(this.Name));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!this.IsMultiplayerAuthority())
            return;

        Vector2 inputDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        var cmd = new UserCommand
        {
            Id = Multiplayer.GetUniqueId(),
            DirX = inputDirection.x,
            DirY = inputDirection.y
        };

        byte[] data = StructHelper.ToByteArray(cmd);

        (Multiplayer as SceneMultiplayer).SendBytes(data, 1, MultiplayerPeer.TransferModeEnum.UnreliableOrdered);
    }
}
