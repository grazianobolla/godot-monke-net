using Godot;
using System.Collections.Generic;
using ImGuiNET;
using System;

/*
    Receives and presents the Player the snapshots emmited by the server.
*/
public partial class SnapshotInterpolator : Node
{
    public int BufferTime { get; set; } // Buffer size in milliseconds

    private readonly List<NetMessage.GameSnapshot> _snapshotBuffer = new();
    private const int RecentPast = 0, NextFuture = 1;
    private float _interpolationFactor = 0;

    public override void _Ready()
    {
        BufferTime = 100; //TODO: magic number
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    public void InterpolateStates(Node playersArray, int currentClock)
    {
        // Point in time to render (in the past)
        double renderTime = currentClock - BufferTime;

        if (_snapshotBuffer.Count > 1)
        {
            // Clear any unwanted (past) states
            while (_snapshotBuffer.Count > 2 && renderTime > _snapshotBuffer[1].Time)
            {
                _snapshotBuffer.RemoveAt(0);
            }

            double timeDiffBetweenStates = _snapshotBuffer[NextFuture].Time - _snapshotBuffer[RecentPast].Time;
            double renderDiff = renderTime - _snapshotBuffer[RecentPast].Time;

            _interpolationFactor = (float)(renderDiff / timeDiffBetweenStates);

            var futureStates = _snapshotBuffer[NextFuture].States;

            for (int i = 0; i < futureStates.Length; i++)
            {
                //TODO: check if the player is aviable in both states
                NetMessage.UserState futureState = _snapshotBuffer[NextFuture].States[i];
                NetMessage.UserState pastState = _snapshotBuffer[RecentPast].States[i];

                var dummy = playersArray.GetNode<Node3D>(futureState.Id.ToString());

                if (dummy.IsMultiplayerAuthority() == false)
                {
                    dummy.Position = pastState.Position.Lerp(futureState.Position, _interpolationFactor);
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

    private void DisplayDebugInformation()
    {
        ImGui.Begin("Snapshot Interpolator Information");
        ImGui.Text($"Interp. Factor {_interpolationFactor}");
        ImGui.Text($"Buffer Size {_snapshotBuffer.Count} snapshots");
        ImGui.Text($"Buffer Time {BufferTime}ms");
        ImGui.End();
    }

}