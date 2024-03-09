using Godot;
using System.Collections.Generic;
using ImGuiNET;

/*
    Receives and presents the Player the snapshots emmited by the server.
*/
public partial class SnapshotInterpolator : Node
{
    [Export] private int _minBufferTime = 3;

    private readonly List<NetMessage.GameSnapshot> _snapshotBuffer = new();
    private const int RecentPast = 0, NextFuture = 1;
    private double _interpolationFactor = 0;
    private int _bufferTime = 0;
    private double _currentTick = 0;

    public override void _Ready()
    {
        _bufferTime = 6; //TODO: magic number
    }

    public override void _Process(double delta)
    {
        _currentTick += delta / PhysicsUtils.FrameTime;
        DisplayDebugInformation();
    }

    public void ProcessTick(int currentTick)
    {
        _currentTick = currentTick;
    }

    public void InterpolateStates(Node playersArray)
    {
        // Point in time to render (in the past)
        double renderTime = _currentTick - _bufferTime;

        if (_snapshotBuffer.Count > 1)
        {
            // Clear any unwanted (past) states
            while (_snapshotBuffer.Count > 2 && renderTime > _snapshotBuffer[1].Time)
            {
                _snapshotBuffer.RemoveAt(0);
            }

            var nextSnapshot = _snapshotBuffer[NextFuture];
            var prevSnapshot = _snapshotBuffer[RecentPast];

            int timeDiffBetweenStates = nextSnapshot.Time - prevSnapshot.Time;
            double renderDiff = renderTime - prevSnapshot.Time;

            _interpolationFactor = renderDiff / timeDiffBetweenStates;

            var futureStates = nextSnapshot.States;

            for (int i = 0; i < futureStates.Length; i++)
            {
                //TODO: check if the player is aviable in both states
                NetMessage.UserState futureState = nextSnapshot.States[i];
                NetMessage.UserState pastState = prevSnapshot.States[i];

                var dummy = playersArray.GetNode<Node3D>(futureState.Id.ToString()); //FIXME: remove GetNode for the love of god

                if (dummy != null && dummy.IsMultiplayerAuthority() == false)
                {
                    dummy.Position = pastState.Position.Lerp(futureState.Position, (float)_interpolationFactor);
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

        if (_interpolationFactor > 1) ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
        ImGui.Text($"Interp. Factor {_interpolationFactor:0.00}");
        ImGui.PopStyleColor();

        ImGui.Text($"Buffer Size {_snapshotBuffer.Count} snapshots");
        ImGui.Text($"Buffer Time {_bufferTime} ticks");
        ImGui.End();
    }

}