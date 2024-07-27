using Godot;
using System;

public partial class FirstPersonCameraController : Node3D
{
	[Export] float mouse_sensitivity = 0.05f;
	[Export] private float _maxVerticalAngle = 90;

	private Node3D rotationHelperY;
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		rotationHelperY = GetParent<Node3D>();
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotionEvent)
		{
			RotateX(-Mathf.DegToRad(mouseMotionEvent.Relative.Y * mouse_sensitivity));
			rotationHelperY.RotateY(Mathf.DegToRad(-mouseMotionEvent.Relative.X * mouse_sensitivity));

			Vector3 cameraRot = RotationDegrees;
			cameraRot.X = Mathf.Clamp(cameraRot.X, -_maxVerticalAngle, _maxVerticalAngle);
			RotationDegrees = cameraRot;
		}

		if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.IsActionPressed("escape"))
			{
				Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Visible ?
					Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
			}
		}

	}

	public float GetLateralRotationAngle()
	{
		return rotationHelperY.Rotation.Y;
	}

	public void RotateCameraLateral(float amount)
	{
		rotationHelperY.RotateY(amount);
	}
}
