using Godot;
using System.Collections.Generic;

public class SnapshotInterpolator
{
    public double BufferTime; // Buffer size in seconds
    private List<GameSnapshot> _snapshotBuffer = new();

    public SnapshotInterpolator(double bufferTime = 0.1f)
    {
        BufferTime = bufferTime;
    }

    public void InterpolateStates(Node playersArray)
    {
        // Point in time to render (in the past)
        // TODO: replace UNIX time with network sychronized time
        double renderTime = Time.GetUnixTimeFromSystem() - BufferTime;

        if (_snapshotBuffer.Count > 1)
        {
            // Clear any unwanted (past) states
            while (_snapshotBuffer.Count > 2 && renderTime > _snapshotBuffer[1].Time)
            {
                _snapshotBuffer.RemoveAt(0);
            }

            double timeDiffBetweenStates = _snapshotBuffer[1].Time - _snapshotBuffer[0].Time;
            double renderDiff = renderTime - _snapshotBuffer[0].Time;

            float interpolationFactor = (float)(renderDiff / timeDiffBetweenStates);

            var futureStates = _snapshotBuffer[1].States;

            for (int i = 0; i < futureStates.Length; i++)
            {
                UserState futureState = _snapshotBuffer[1].States[i];
                UserState pastState = _snapshotBuffer[0].States[i];

                var player = playersArray.GetNode<Player>(futureState.Id.ToString());
                player.Position = pastState.Position.Lerp(futureState.Position, interpolationFactor);
            }
        }
    }

    public void PushState(GameSnapshot snapshot)
    {
        if (_snapshotBuffer.Count <= 0 || snapshot.Time > _snapshotBuffer[_snapshotBuffer.Count - 1].Time)
        {
            _snapshotBuffer.Add(snapshot);
        }
    }

    public int BufferCount
    {
        get => _snapshotBuffer.Count;
    }
}