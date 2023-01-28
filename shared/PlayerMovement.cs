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

    public static Vector3 ComputeMotion(Vector2 input)
    {
        return new Vector3(input.X, 0, input.Y).Normalized() * (1 / 60.0f) * 5;
    }
}