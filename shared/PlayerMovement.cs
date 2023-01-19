using Godot;

public partial class PlayerMovement : Node
{
    private CharacterBody3D _body;

    private const float Speed = 5.0f;
    private const float JumpVelocity = 9f;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody3D>();
    }

    public void CalculateMovement(double delta, Vector2 inputDir, bool isJumping)
    {
        Vector3 _velocity = _body.Velocity;

        // Add the gravity
        if (!_body.IsOnFloor())
            _velocity.y -= 9 * (float)delta;

        // Handle Jump
        if (isJumping && _body.IsOnFloor())
            _velocity.y = JumpVelocity;

        Vector3 direction = (_body.Transform.basis * new Vector3(inputDir.x, 0, inputDir.y)).Normalized();

        if (direction != Vector3.Zero)
        {
            _velocity.x = direction.x * Speed;
            _velocity.z = direction.z * Speed;
        }
        else
        {
            _velocity.x = Mathf.MoveToward(_body.Velocity.x, 0, Speed);
            _velocity.z = Mathf.MoveToward(_body.Velocity.z, 0, Speed);
        }

        _body.Velocity = _velocity;
        _body.MoveAndSlide();
    }
}