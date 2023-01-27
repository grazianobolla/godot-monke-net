using Godot;
using System.Collections.Generic;

public class SnapshotInterpolator
{
    public int BufferTime; // Buffer size in milliseconds
    public float InterpolationFactor { get; private set; }

    private List<NetMessage.GameSnapshot> _snapshotBuffer = new();
    private const int RecentPast = 0, NextFuture = 1;
    private const int InitialBufferTime = 100;

    public SnapshotInterpolator()
    {
        BufferTime = InitialBufferTime;
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

            double timeDiffBetweenStates = _snapshotBuffer[NextFuture].Time - _snapshotBuffer[RecentPast].Time;
            double renderDiff = renderTime - _snapshotBuffer[RecentPast].Time;

            InterpolationFactor = (float)(renderDiff / timeDiffBetweenStates);

            var futureStates = _snapshotBuffer[NextFuture].States;

            for (int i = 0; i < futureStates.Length; i++)
            {
                //TODO: check if the player is aviable in both states
                NetMessage.UserState futureState = _snapshotBuffer[NextFuture].States[i];
                NetMessage.UserState pastState = _snapshotBuffer[RecentPast].States[i];

                var dummy = playersArray.GetNode<Node3D>(futureState.Id.ToString());

                if (dummy.IsMultiplayerAuthority() == false)
                {
                    dummy.Position = pastState.Position.Lerp(futureState.Position, InterpolationFactor);
                }
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