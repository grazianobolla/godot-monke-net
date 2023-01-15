using Godot;
using System;
using System.Collections.Generic;

public class StateInterpolator
{
    public double BufferTime = 0.1; // Buffer size in seconds
    private List<UserState> _statesBuffer = new();

    public StateInterpolator(double bufferTime = 0.1f)
    {
        BufferTime = bufferTime;
    }

    public void InterpolateStates(Node3D character)
    {
        // Point in time to render (in the past)
        // TODO: replace UNIX time with network sychronized time
        double renderTime = Time.GetUnixTimeFromSystem() - BufferTime;

        if (_statesBuffer.Count > 1)
        {
            // Clear any unwanted (past) states
            while (_statesBuffer.Count > 2 && renderTime > _statesBuffer[1].Time)
            {
                _statesBuffer.RemoveAt(0);
            }

            double timeDiffBetweenStates = _statesBuffer[1].Time - _statesBuffer[0].Time;
            double renderDiff = renderTime - _statesBuffer[0].Time;

            float interpolationFactor = (float)(renderDiff / timeDiffBetweenStates);

            var pastPos = new Vector3(_statesBuffer[0].X, _statesBuffer[0].Y, _statesBuffer[0].Z);
            var futurePos = new Vector3(_statesBuffer[1].X, _statesBuffer[1].Y, _statesBuffer[1].Z);

            character.Position = pastPos.Lerp(futurePos, interpolationFactor);
        }
    }

    public void PushState(UserState state)
    {
        if (_statesBuffer.Count <= 0)
        {
            _statesBuffer.Add(state);
            return;
        }

        if (state.Time > _statesBuffer[_statesBuffer.Count - 1].Time)
        {
            _statesBuffer.Add(state);
            return;
        }

        GD.PrintErr("Received out of order packet, ignoring (", state.Time, " vs ", _statesBuffer[_statesBuffer.Count - 1].Time, ")");
    }
}