using System;
using Godot;

public class ClientClock
{
    public int Ticks { get; private set; } = 0;

    private double _decimalCollector = 0;

    public void Setup(int serverTicks, int latency)
    {
        Ticks = serverTicks + latency;
        GD.Print($"Client clock synced to {Ticks}");
    }

    public void AdjustClock(double delta, int deltaLatency)
    {
        int msDelta = (int)(delta * 1000.0);

        Ticks += msDelta + deltaLatency;

        // Prevent clock drift
        _decimalCollector += (delta * 1000.0) - msDelta;
        if (_decimalCollector >= 1.00)
        {
            Ticks += 1;
            _decimalCollector -= 1.0;
        }
    }
}