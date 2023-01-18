using Godot;
using System.Collections.Generic;

public class SnapshotInterpolator
{
    public int BufferTime; // Buffer size in milliseconds
    public float InterpolationFactor { get; private set; }

    private List<NetMessage.GameSnapshot> _snapshotBuffer = new();
    private const int RECENT_PAST = 0, NEXT_FUTURE = 1;

    public SnapshotInterpolator(int bufferTime)
    {
        BufferTime = bufferTime;
    }

    public void InterpolateStates(Node playersArray, int clock)
    {
        // Point in time to render (in the past)
        double renderTime = clock - BufferTime;

        if (_snapshotBuffer.Count > 1)
        {
            // Clear any unwanted (past) states
            while (_snapshotBuffer.Count > 2 && renderTime > _snapshotBuffer[1].Time)
            {
                _snapshotBuffer.RemoveAt(0);
            }

            double timeDiffBetweenStates = _snapshotBuffer[NEXT_FUTURE].Time - _snapshotBuffer[RECENT_PAST].Time;
            double renderDiff = renderTime - _snapshotBuffer[RECENT_PAST].Time;

            InterpolationFactor = (float)(renderDiff / timeDiffBetweenStates);

            var futureStates = _snapshotBuffer[NEXT_FUTURE].States;

            for (int i = 0; i < futureStates.Length; i++)
            {
                //TODO: check if the player is aviable in both states
                NetMessage.UserState futureState = _snapshotBuffer[NEXT_FUTURE].States[i];
                NetMessage.UserState pastState = _snapshotBuffer[RECENT_PAST].States[i];

                var player = playersArray.GetNode<Node3D>(futureState.Id.ToString());
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