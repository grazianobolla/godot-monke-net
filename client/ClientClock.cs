using System;
using Godot;

public class ClientClock
{
    public static int Ticks { get; private set; } = 0;

    private double _decimalCollector = 0;

    public void Setup(int serverTicks, int offset)
    {
        Ticks = serverTicks + offset;
        GD.Print($"Client clock synced to {Ticks}");
    }

    public void AdjustClock(double delta, int offset)
    {
        int msDelta = (int)(delta * 1000.0);

        Ticks += msDelta + offset;

        // Prevent clock drift
        _decimalCollector += (delta * 1000.0) - msDelta;
        if (_decimalCollector >= 1.00)
        {
            Ticks += 1;
            _decimalCollector -= 1.0;
        }
    }
}