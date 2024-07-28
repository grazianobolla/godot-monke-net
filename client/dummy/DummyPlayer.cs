using Godot;
using NetMessage;

public partial class DummyPlayer : Node3D, IInterpolatedEntity
{
	public void HandleStateInterpolation(EntityState pastState, EntityState futureState, float interpolationFactor)
	{
		Position = pastState.Position.Lerp(futureState.Position, interpolationFactor);

		var pastRot = Rotation;
		pastRot.Y = pastState.LateralLookAngle;

		var newRot = Rotation;
		pastRot.Y = futureState.LateralLookAngle;

		Rotation = pastRot.Lerp(newRot, interpolationFactor);
	}

}
