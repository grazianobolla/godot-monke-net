using Godot;

public partial class PlayerMovement : Node
{
    private CharacterBody3D _body;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody3D>();
    }

    public static Vector3 ComputeMotion(CharacterBody3D body, Vector3 velocity, Vector2 input, double delta)
    {
        Vector3 direction = new Vector3(input.X, 0, input.Y).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * 5;
            velocity.Z = direction.Z * 5;
        }
        else
        {
            velocity *= 0.9f;
        }

        KinematicCollision3D coll = body.MoveAndCollide(velocity * (float)delta, true);

        if (coll != null)
        {
            velocity = velocity.Slide(coll.GetNormal());
        }

        return velocity;
    }
}