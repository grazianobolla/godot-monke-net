using MonkeNet.Shared;

namespace MonkeNet.Client;

public interface IInterpolatedEntity
{
    public void HandleStateInterpolation(IEntityStateMessage past, IEntityStateMessage future, float interpolationFactor);
}