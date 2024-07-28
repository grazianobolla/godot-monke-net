using Godot;
using System.Collections.Generic;
using ImGuiNET;

/*
    Receives and presents the Player the snapshots emmited by the server.
*/
public partial class SnapshotInterpolator : NetworkedNode
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
        base._Ready();
        _bufferTime = 6; //TODO: magic number
    }

    public override void _Process(double delta)
    {
        _currentTick += delta / PhysicsUtils.FrameTime;
        double tickToProcess = _currentTick - _bufferTime; // (Current tick - _bufferTime) the point in time in the past which we want to render
        InterpolateStates(tickToProcess);
    }

    protected override void OnProcessTick(int currentTick, int currentRemoteTick)
    {
        _currentTick = currentTick;
    }

    protected override void OnServerPacketReceived(NetMessage.ICommand command)
    {
        if (command is NetMessage.GameSnapshot snapshot)
        {
            // Add snapshot tu buffer if we don't have any or if it is a future one
            if (_snapshotBuffer.Count <= 0 || snapshot.Tick > _snapshotBuffer[^1].Tick)
                _snapshotBuffer.Add(snapshot);
        }
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

                    var entity = _entityArray.GetNodeOrNull<Node>(futureState.Id.ToString()); //FIXME: remove GetNode for the love of god

                    if (entity != null && entity is IInterpolatedEntity interpolatedEntity)
                    {
                        interpolatedEntity.HandleStateInterpolation(pastState, futureState, (float)_interpolationFactor);
                    }
                }
            }
        }
    }

    public void SetBufferTime(int bufferTime)
    {
        _bufferTime = bufferTime + _minBufferTime;
    }

    public void DisplayDebugInformation()
    {
        if (ImGui.CollapsingHeader("Snapshot Interpolator Information"))
        {
            if (_interpolationFactor > 1) ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
            ImGui.Text($"Interp. Factor {_interpolationFactor:0.00}");
            ImGui.PopStyleColor();

            ImGui.Text($"Buffer Size {_snapshotBuffer.Count} snapshots");
            ImGui.Text($"Buffer Time {_bufferTime} ticks");

            int bufferTimeMs = (int)(_bufferTime * PhysicsUtils.FrameTime * 1000);
            ImGui.Text($"World State is {bufferTimeMs}ms in the past");
        }
    }
}