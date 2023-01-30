using Godot;

public partial class PlayerMovement : Node
{
    private CharacterBody3D _body;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody3D>();
    }

    public static Vector3 ComputeMotion(Vector2 input)
    {
        return new Vector3(input.X, 0, input.Y).Normalized() * (1 / 30.0f) * 5;
    }
}