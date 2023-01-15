using Godot;
using System;

// Wrapper scene spawned by the MultiplayerSpawner
public partial class Player : Node3D
{
    private StateInterpolator _stateInterpolator = new();

    public override void _Ready()
    {
        this.SetMultiplayerAuthority(Int32.Parse(this.Name));
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.GetUniqueId() == 1)
            return;

        _stateInterpolator.InterpolateStates(this);
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

        (Multiplayer as SceneMultiplayer).SendBytes(data, 1,
            MultiplayerPeer.TransferModeEnum.Unreliable);
    }

    public void ReceiveState(UserState state)
    {
        _stateInterpolator.PushState(state);
    }
}
