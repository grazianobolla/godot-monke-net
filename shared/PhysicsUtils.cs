
using Godot;

public class PhysicsUtils
{
    public static readonly float FrameTimeInMsec = (1.0f / Engine.PhysicsTicksPerSecond) * 1000.0f;

    public static int TickToMsec(int tick)
    {
        return Mathf.RoundToInt(tick * FrameTimeInMsec);
    }

    public static int MsecToTick(int msec)
    {
        return Mathf.RoundToInt(msec / FrameTimeInMsec);
    }
}