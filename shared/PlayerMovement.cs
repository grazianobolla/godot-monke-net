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

    public static Vector3 ComputeMotion(Rid rid, Transform3D from, Vector3 velocity, Vector2 input, double delta)
    {
        Vector3 direction = new Vector3(input.X, 0, input.Y).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * SPEED;
            velocity.Z = direction.Z * SPEED;
        }
        else
        {
            velocity *= 0.85f;
        }

        var testParameters = new PhysicsTestMotionParameters3D();
        testParameters.From = from;
        testParameters.Motion = velocity * (float)delta;

        var collResult = new PhysicsTestMotionResult3D();

        bool hasCollided = PhysicsServer3D.BodyTestMotion(rid, testParameters, collResult);

        if (hasCollided)
        {
            velocity = velocity.Slide(collResult.GetCollisionNormal());
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