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
        return new Vector3(input.x, 0, input.y).Normalized() * (1 / 30.0f) * 5;
    }
}