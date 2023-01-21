using Godot;

public partial class PlayerMovement : Node
{
    private CharacterBody3D _body;

    private const float MaxSpeed = 5.0f;
    private const float JumpVelocity = 9f;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody3D>();
    }

    public static Vector3 ComputeMotion(Vector3 velocity, Vector2 input, bool onFloor, bool isJumping, double delta)
    {
        // Add the gravity
        if (!onFloor)
            velocity.y -= 9 * (float)delta;

        // Handle Jump
        if (isJumping && onFloor)
            velocity.y = JumpVelocity;

        Vector3 direction = new Vector3(input.x, 0, input.y).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.x = direction.x * MaxSpeed;
            velocity.z = direction.z * MaxSpeed;
        }
        else
        {
            velocity *= 0.85f;
        }

        return velocity;
    }
}