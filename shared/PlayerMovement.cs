using Godot;
using NetMessage;

public partial class PlayerMovement : Node
{
    private CharacterBody3D _body;
    private const int SPEED = 5;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody3D>();
    }

    public static Vector3 ComputeMotion(CharacterBody3D body, Vector3 velocity, Vector2 input, double delta)
    {
        Vector3 direction = new Vector3(input.X, 0, input.Y).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * SPEED;
            velocity.Z = direction.Z * SPEED;
        }
        else
        {
            velocity *= 0.89f;
        }

        KinematicCollision3D coll = body.MoveAndCollide(velocity * (float)delta, true);

        if (coll != null)
        {
            velocity = velocity.Slide(coll.GetNormal());
        }

        return velocity;
    }

    public static Vector2 InputToDirection(byte input)
    {
        Vector2 direction = Vector2.Zero;

        if ((input & (byte)InputFlags.Right) > 0) direction.X += 1;
        if ((input & (byte)InputFlags.Left) > 0) direction.X -= 1;
        if ((input & (byte)InputFlags.Backward) > 0) direction.Y += 1;
        if ((input & (byte)InputFlags.Forward) > 0) direction.Y -= 1;

        return direction.Normalized();
    }
}