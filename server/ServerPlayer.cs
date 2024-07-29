using Godot;

public partial class ServerPlayer : CharacterBody3D
{
	public int PlayerId { get; set; } = 0; // Network Id for this player
	public float LateralLookAngle { get; set; } = 0;

	public NetMessage.EntityState GetCurrentState()
	{
		return new NetMessage.EntityState
		{
			Id = PlayerId,
			PosArray = [this.Position.X, this.Position.Y, this.Position.Z],
			VelArray = [this.Velocity.X, this.Velocity.Y, this.Velocity.Z],
			LateralLookAngle = LateralLookAngle
		};
	}
}
