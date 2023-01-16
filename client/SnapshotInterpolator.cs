using Godot;
using System.Collections.Generic;

public class SnapshotInterpolator
{
    public int BufferTime; // Buffer size in milliseconds
    public float InterpolationFactor { get; private set; }

    private List<NetMessage.GameSnapshot> _snapshotBuffer = new();

    public SnapshotInterpolator(int bufferTime)
    {
        BufferTime = bufferTime;
    }

    public void InterpolateStates(Node playersArray, int clock)
    {
        // Point in time to render (in the past)
        // TODO: replace UNIX time with network sychronized time
        double renderTime = clock - BufferTime;

        if (_snapshotBuffer.Count > 1)
        {
            // Clear any unwanted (past) states
            while (_snapshotBuffer.Count > 2 && renderTime > _snapshotBuffer[1].Time)
            {
                _snapshotBuffer.RemoveAt(0);
            }

            double timeDiffBetweenStates = _snapshotBuffer[1].Time - _snapshotBuffer[0].Time;
            double renderDiff = renderTime - _snapshotBuffer[0].Time;

            InterpolationFactor = (float)(renderDiff / timeDiffBetweenStates);

            var futureStates = _snapshotBuffer[1].States;

            for (int i = 0; i < futureStates.Length; i++)
            {
                NetMessage.UserState futureState = _snapshotBuffer[1].States[i];
                NetMessage.UserState pastState = _snapshotBuffer[0].States[i];

                var player = playersArray.GetNode<Player>(futureState.Id.ToString());
                player.Position = pastState.Position.Lerp(futureState.Position, InterpolationFactor);
            }
        }
    }

    public void PushState(NetMessage.GameSnapshot snapshot)
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