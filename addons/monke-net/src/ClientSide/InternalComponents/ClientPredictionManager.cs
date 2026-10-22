using Godot;
using ImGuiNET;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;
using System.Collections.Generic;

namespace MonkeNet.Client;

/// <summary>
/// Stores predicted game states for entities, upon receiving an snapshot, will check for deviation and perform rollback and re-simulation if needed.
/// </summary>
[GlobalClass]
public partial class ClientPredictionManager : InternalClientComponent
{
    private readonly List<PredictedState> _predictedStates = [];
    private int _lastTickReceived = 0;
    private int _lastPredictedTick = 0;
    private int _misspredictionsCount = 0;
    private int _missedLocalState = 0;

    protected override void OnCommandReceived(IPackableMessage command)
    {
        if (!NetworkReady)
            return;

        if (command is GameSnapshotMessage snapshot)
        {
            if (snapshot.Tick > _lastTickReceived)
            {
                _lastTickReceived = snapshot.Tick;
                ProcessServerState(snapshot);
            }
        }
    }

    public void Predict(int tick, IPackableElement input)
    {
        _lastPredictedTick = tick;

        MonkeNetManager.Instance.EntitySpawner.Entities.ForEach(entity =>
        {
            var clientPredictedEntity = entity.GetComponent<ClientPredictedEntity>();
            clientPredictedEntity?.OnProcessTick(tick, input);

            var clientExtrapolatedEntity = entity.GetComponent<ClientExtrapolatedEntity>();
            clientExtrapolatedEntity?.OnExtrapolateTick();
        });
    }

    public void RegisterPrediction(int tick, IPackableElement input)
    {
        var predictedState = new PredictedState
        {
            Tick = tick,
            Input = input,
            Entities = []
        };

        _predictedStates.Add(predictedState);

        //TODO: use array of ClientPredictedEntity that updates each time a new entity is spawned/despawned
        //TODO: store entity state inside entity itself instead of having everything here on PredictionManager
        MonkeNetManager.Instance.EntitySpawner.Entities.ForEach(entity =>
        {
            var clientPredictedEntity = entity.GetComponent<ClientPredictedEntity>();
            if (clientPredictedEntity != null)
            {
                predictedState.Entities.Add(clientPredictedEntity, clientPredictedEntity.GetPosition());
            }
        });
    }

    private void ProcessServerState(GameSnapshotMessage receivedSnapshot)
    {
        ExtrapolateEntities(receivedSnapshot);

        var predictedStateData = _predictedStates.Find(prediction => prediction.Tick == receivedSnapshot.Tick);
        _predictedStates.RemoveAll(predictedState => predictedState.Tick <= receivedSnapshot.Tick);

        if (predictedStateData == default(PredictedState) || predictedStateData.Tick != receivedSnapshot.Tick)
        {
            _missedLocalState++;
            return;
        }

        // Iterate all entities saved for the tick
        foreach (ClientPredictedEntity predictableEntity in predictedStateData.Entities.Keys)
        {
            // Get predicted and authoritative state for the entity
            var predictedState = predictedStateData.Entities[predictableEntity];
            var authoritativeState = FindStateForEntityId(predictableEntity.EntityId, receivedSnapshot.States);

            if (predictableEntity.HasMisspredicted(receivedSnapshot.Tick, authoritativeState, predictedState))
            {
                _misspredictionsCount++;
                RollbackAndResimulate(receivedSnapshot.States, predictedStateData);
                return;
            }
        }
    }

    // Move extrapolated entities to the authoritative state and advance them up to the current local tick, they are never rolled back
    private void ExtrapolateEntities(GameSnapshotMessage receivedSnapshot)
    {
        int ticksToExtrapolate = System.Math.Max(0, _lastPredictedTick - receivedSnapshot.Tick);

        foreach (IEntityStateData authoritativeState in receivedSnapshot.States)
        {
            var clientExtrapolatedEntity = EntitySpawner.Instance.GetEntityById(authoritativeState.EntityId)
                .GetComponent<ClientExtrapolatedEntity>();

            if (clientExtrapolatedEntity == null)
                continue;

            clientExtrapolatedEntity.HandleAuthoritativeState(authoritativeState);
            for (int i = 0; i < ticksToExtrapolate; i++)
            {
                clientExtrapolatedEntity.OnExtrapolateTick();
            }
        }
    }

    private void RollbackAndResimulate(IEntityStateData[] authoritativeStates, PredictedState predictedStateData)
    {
        // Set all entities to authoritative state
        foreach (ClientPredictedEntity predictableEntity in predictedStateData.Entities.Keys)
        {
            var authoritativeState = FindStateForEntityId(predictableEntity.EntityId, authoritativeStates);
            predictableEntity.HandleReconciliation(authoritativeState);
        }

        // Advance simulation forward for all remaining inputs
        for (int i = 0; i < _predictedStates.Count; i++)
        {
            var remainingInput = _predictedStates[i];
            foreach (ClientPredictedEntity predictableEntity in remainingInput.Entities.Keys)
            {
                predictableEntity.ResimulateTick(remainingInput.Input);
            }

            PhysicsServer3D.SpaceStep(MonkeNetManager.Instance.PhysicsSpace, PhysicsUtils.DeltaTime);
            PhysicsServer3D.SpaceFlushQueries(MonkeNetManager.Instance.PhysicsSpace);

            foreach (ClientPredictedEntity predictableEntity in remainingInput.Entities.Keys)
            {
                remainingInput.Entities[predictableEntity] = predictableEntity.GetPosition();
            }
        }
    }

    private static IEntityStateData FindStateForEntityId(int entityId, IEntityStateData[] authStates)
    {
        foreach (IEntityStateData state in authStates)
        {
            if (state.EntityId == entityId)
            {
                return state;
            }
        }

        return null;
    }

    public void DisplayDebugInformation()
    {
        if (ImGui.CollapsingHeader("Prediction Manager"))
        {
            ImGui.Text($"Misspredictions: {_misspredictionsCount}");
            ImGui.Text($"Missed Local States: {_missedLocalState}");
            ImGui.Text($"Prediction History: {_predictedStates.Count}");
        }
    }

    private class PredictedState
    {
        public int Tick;                                            // Tick at which the input was taken
        public IPackableElement Input;                              // Input message sent to the server
        public Dictionary<ClientPredictedEntity, Vector3> Entities;
    }
}
