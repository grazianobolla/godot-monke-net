using Godot;
using System.Collections.Generic;
using ImGuiNET;
using System;

/*
    Receives and presents the Player the snapshots emmited by the server.
*/
public partial class SnapshotInterpolator : Node
{
    [Export] private int _minBufferTime = (int)PhysicsUtils.FrameTimeInMsec;

    private readonly List<NetMessage.GameSnapshot> _snapshotBuffer = new();
    private const int RecentPast = 0, NextFuture = 1;
    private float _interpolationFactor = 0;
    private int _bufferTime = 0;

    public override void _Ready()
    {
        _bufferTime = 100; //TODO: magic number
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    public void InterpolateStates(Node playersArray, int currentTimeMsec)
    {
        // Point in time to render (in the past)
        int renderTime = currentTimeMsec - _bufferTime;

        if (_snapshotBuffer.Count > 1)
        {
            // Clear any unwanted (past) states
            while (_snapshotBuffer.Count > 2 && renderTime > PhysicsUtils.TickToMsec(_snapshotBuffer[1].Time))
            {
                _snapshotBuffer.RemoveAt(0);
            }

            var nextSnapshot = _snapshotBuffer[NextFuture];
            var prevSnapshot = _snapshotBuffer[RecentPast];
            int nextSnapshotTickInMsec = PhysicsUtils.TickToMsec(nextSnapshot.Time);
            int prevSnapshotTickInMsec = PhysicsUtils.TickToMsec(prevSnapshot.Time);

            int timeDiffBetweenStates = nextSnapshotTickInMsec - prevSnapshotTickInMsec;
            int renderDiff = renderTime - prevSnapshotTickInMsec;

            _interpolationFactor = renderDiff / (float)timeDiffBetweenStates;

            var futureStates = nextSnapshot.States;

            for (int i = 0; i < futureStates.Length; i++)
            {
                //TODO: check if the player is aviable in both states
                NetMessage.UserState futureState = nextSnapshot.States[i];
                NetMessage.UserState pastState = prevSnapshot.States[i];

                var dummy = playersArray.GetNode<Node3D>(futureState.Id.ToString()); //FIXME: remove GetNode for the love of god

                if (dummy != null && dummy.IsMultiplayerAuthority() == false)
                {
                    dummy.Position = pastState.Position.Lerp(futureState.Position, _interpolationFactor);
                }
            }
        }
    }

    public void PushState(NetMessage.GameSnapshot snapshot)
    {
        if (_snapshotBuffer.Count <= 0 || snapshot.Time > _snapshotBuffer[^1].Time)
        {
            _snapshotBuffer.Add(snapshot);
        }
    }

    public void SetBufferTime(int bufferTime)
    {
        _bufferTime = bufferTime + _minBufferTime;
    }

    private void DisplayDebugInformation()
    {
        ImGui.Begin("Snapshot Interpolator Information");
        ImGui.Text($"Interp. Factor {_interpolationFactor}");
        ImGui.Text($"Buffer Size {_snapshotBuffer.Count} snapshots");
        ImGui.Text($"Buffer Time {_bufferTime}ms");
        ImGui.End();
    }

}