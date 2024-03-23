using Godot;
using System;

public partial class RotationHelper : Node3D
{
    [Export] float mouse_sensitivity = 0.05f;
    [Export] private float _maxVerticalAngle = 90;

    CharacterBody3D player;
    CollisionShape3D playerCollisionShape;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        player = GetParent<CharacterBody3D>();
        playerCollisionShape = player.GetNode<CollisionShape3D>("CollisionShape3D");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {
            RotateX(-Mathf.DegToRad(mouseEvent.Relative.Y * mouse_sensitivity));
            player.RotateY(Mathf.DegToRad(-mouseEvent.Relative.X * mouse_sensitivity));

            Vector3 cameraRot = RotationDegrees;
            cameraRot.X = Mathf.Clamp(cameraRot.X, -_maxVerticalAngle, _maxVerticalAngle);
            RotationDegrees = cameraRot;

            // Reset Collider
            playerCollisionShape.GlobalRotation = Vector3.Zero;
        }
    }
}