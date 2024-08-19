using Godot;
using NetMessage;

namespace Client;

public partial class DummyPlayer : Node3D, IInterpolatedEntity
{
	public void HandleStateInterpolation(EntityState pastState, EntityState futureState, float interpolationFactor)
	{
		Position = pastState.Position.Lerp(futureState.Position, interpolationFactor);
		var rotation = Mathf.LerpAngle(pastState.LateralLookAngle, futureState.LateralLookAngle, interpolationFactor);
		this.Rotation = Vector3.Up * rotation;
	}
}