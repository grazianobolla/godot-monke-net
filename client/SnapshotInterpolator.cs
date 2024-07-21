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
    private int _bufferTime = 0;                // How many ticks in the past we are rendering the world state
    private double _currentTick = 0;            // Current local tick
    private Node _entityArray;

    public override void _Ready()
    {
        _bufferTime = 6; //TODO: magic number
    }

    public override void _Process(double delta)
    {
        _currentTick += delta / PhysicsUtils.FrameTime;
        double tickToProcess = _currentTick - _bufferTime; // (Current tick - _bufferTime) the point in time in the past which we want to render
        InterpolateStates(tickToProcess);
        DisplayDebugInformation();
    }

    public void ProcessTick(int currentTick)
    {
        _currentTick = currentTick;
    }

    public void SetEntityArray(Node entities)
    {
        this._entityArray = entities;
    }

    private void InterpolateStates(double renderTick)
    {
        if (_snapshotBuffer.Count > 1)
        {
            // Clear any unwanted (past) states
            while (_snapshotBuffer.Count > 2 && renderTick > _snapshotBuffer[1].Tick)
            {
                _snapshotBuffer.RemoveAt(0);
            }

            var nextSnapshot = _snapshotBuffer[NextFuture];
            var prevSnapshot = _snapshotBuffer[RecentPast];

            int timeDiffBetweenStates = nextSnapshot.Tick - prevSnapshot.Tick;
            double renderDiff = renderTick - prevSnapshot.Tick;

            _interpolationFactor = renderDiff / timeDiffBetweenStates;

            var futureStates = nextSnapshot.States;

            for (int i = 0; i < futureStates.Length; i++)
            {
                if (nextSnapshot.States.Length > i && prevSnapshot.States.Length > i)
                {
                    NetMessage.EntityState futureState = nextSnapshot.States[i];
                    NetMessage.EntityState pastState = prevSnapshot.States[i];

                    var dummy = _entityArray.GetNodeOrNull<Node3D>(futureState.Id.ToString()); //FIXME: remove GetNode for the love of god

                    if (dummy != null && dummy.IsMultiplayerAuthority() == false)
                    {
                        dummy.Position = pastState.Position.Lerp(futureState.Position, (float)_interpolationFactor);
                    }
                }
            }
        }
    }

    public void PushState(NetMessage.GameSnapshot snapshot)
    {
        if (_snapshotBuffer.Count <= 0 || snapshot.Tick > _snapshotBuffer[^1].Tick)
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

        int bufferTimeMs = (int)(_bufferTime * PlayerMovement.FrameDelta * 1000);
        ImGui.Text($"World State is {bufferTimeMs}ms in the past");
        ImGui.End();
    }

}