using Godot;
using MonkeNet.Server;

namespace GameDemo;

// Server Player movement script
public partial class ServerPlayerMovement : CharacterControllerServer<CharacterInputMessage>
{
    private ServerPlayer _player;

    public override void _Ready()
    {
        base._Ready();
        _player = GetParent<ServerPlayer>();
    }

    // Called each time we receive a PlayerInputMessage from the client, we move the player accordingly (same code is run on the client)
    protected override Vector3 CalculateVelocity(CharacterBody3D body, CharacterInputMessage input)
    {
        _player.Yaw = input.CameraYaw;
        return PlayerMovementCalculator.CalculateVelocity(body, input);
    }
}
