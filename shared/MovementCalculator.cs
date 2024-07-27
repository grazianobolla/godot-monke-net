using Godot;
using NetMessage;

public static class MovementCalculator
{
    public static readonly float MaxRunSpeed = 5;
    public static readonly float MaxWalkSpeed = 2;
    public static readonly float Gravity = 9.8f;
    public static readonly float JumpVelocity = 2.0f;

    public static Vector3 ComputeVelocity(CharacterBody3D body, NetMessage.UserInput input)
    {
        Vector2 direction2D = InputToDirection(input.Keys);

        bool isWalking = ReadInput(input.Keys, NetMessage.InputFlags.Shift);
        bool isJumping = ReadInput(input.Keys, NetMessage.InputFlags.Space);
        Vector3 velocity = body.Velocity;

        bool isOnFloor = body.IsOnFloor();
        Vector3 direction = new Vector3(direction2D.X, 0, direction2D.Y).Normalized();
        direction = direction.Rotated(Vector3.Up, input.LateralLookAngle);

        if (!direction.IsZeroApprox())
        {
            velocity.X = direction.X * (isWalking ? MaxWalkSpeed : MaxRunSpeed);
            velocity.Z = direction.Z * (isWalking ? MaxWalkSpeed : MaxRunSpeed);
        }
        else
        {
            velocity.X = 0;
            velocity.Z = 0;
        }

        if (!isOnFloor)
            velocity.Y -= Gravity * PhysicsUtils.FrameTime;

        if (isJumping && isOnFloor)
            velocity.Y = JumpVelocity;

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

    public static bool ReadInput(byte input, InputFlags flag)
    {
        return (input & (byte)flag) > 0;
    }
}