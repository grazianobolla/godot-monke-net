
using Godot;

public class PhysicsUtils
{
    public static readonly float FrameTimeInMsec = (1.0f / Engine.PhysicsTicksPerSecond) * 1000.0f;
    public static readonly float FrameTime = 1.0f / Engine.PhysicsTicksPerSecond;

    public static int MsecToTick(int msec)
    {
        return Mathf.CeilToInt(msec / FrameTimeInMsec);
    }
}