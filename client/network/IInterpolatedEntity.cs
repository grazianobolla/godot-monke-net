using Godot;

public interface IInterpolatedEntity
{
    public void HandleStateInterpolation(NetMessage.EntityState pastState, NetMessage.EntityState futureState, float interpolationFactor);
}