using Godot;
using ImGuiNET;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;
using System.Collections.Generic;

namespace MonkeNet.Client;

/// <summary>
/// Stores predicted game states for entities and, upon receiving an snapshot, will check for deviation and perform rollback and re-simulation if needed.
/// </summary>
public partial class SnapshotRollbacker : ClientNetworkNode
{
    private readonly List<PredictedState> _predictedStates = [];
    private int _lastTickReceived = 0;
    private int _misspredictionsCount = 0;
    private int _missedLocalState = 0;

    protected override void OnCommandReceived(IPackableMessage command)
    {
        if (!NetworkReady)
            return;

        if (command is GameSnapshot snapshot)
        {
            if (snapshot.Tick > _lastTickReceived)
            {
                _lastTickReceived = snapshot.Tick;
                ProcessServerState(snapshot);
            }
        }
    }

    public void RegisterPrediction(int tick, IClientInputData input)
    {
        var predictedState = new PredictedState
        {
            Tick = tick,
            Input = input,
            Entities = []
        };

        _predictedStates.Add(predictedState);

        //TODO: use array of IPredictableEntity that updates each time a new entity is spawned/despawned
        //TODO: store entity state inside entity itself instead of having everything here on SnapshotRollbacker
        MonkeNetConfig.Instance.EntitySpawner.Entities.ForEach(entity =>
        {
            if (entity is IPredictableEntity predictableEntity)
            {
                predictedState.Entities.Add(predictableEntity, predictableEntity.Position);
            }
        });
    }

    private void ProcessServerState(GameSnapshot receivedSnapshot)
    {
        var predictedStateData = _predictedStates.Find(prediction => prediction.Tick == receivedSnapshot.Tick);
        _predictedStates.RemoveAll(predictedState => predictedState.Tick <= receivedSnapshot.Tick);

        if (predictedStateData == default(PredictedState) || predictedStateData.Tick != receivedSnapshot.Tick)
        {
            _missedLocalState++;
            return;
        }

        // Iterate all entities saved for the tick
        foreach (IPredictableEntity predictableEntity in predictedStateData.Entities.Keys)
        {
            // Get predicted and authoritative state for the entity
            var predictedState = predictedStateData.Entities[predictableEntity];
            var authoritativeState = FindStateForEntityId(predictableEntity.EntityId, receivedSnapshot.States);

            if (predictableEntity.HasMisspredicted(authoritativeState, predictedState))
            {
                _misspredictionsCount++;
                RollbackAndResimulate(receivedSnapshot.States, predictedStateData);
                return;
            }
        }
    }

    private void RollbackAndResimulate(IEntityStateMessage[] authoritativeStates, PredictedState predictedStateData)
    {
        // Set all entities to authoritative state
        foreach (IPredictableEntity predictableEntity in predictedStateData.Entities.Keys)
        {
            var authoritativeState = FindStateForEntityId(predictableEntity.EntityId, authoritativeStates);
            predictableEntity.HandleReconciliation(authoritativeState);
        }

        // Advance simulation forward for all remaining inputs
        for (int i = 0; i < _predictedStates.Count; i++)
        {
            var remainingInput = _predictedStates[i];
            foreach (IPredictableEntity predictableEntity in remainingInput.Entities.Keys)
            {
                predictableEntity.ResimulateTick(remainingInput.Input);
            }

            PhysicsServer3D.Singleton.Call("space_step", MonkeNetManager.Instance.PhysicsSpace, PhysicsUtils.DeltaTime);
            PhysicsServer3D.Singleton.Call("space_flush_queries", MonkeNetManager.Instance.PhysicsSpace);

            foreach (IPredictableEntity predictableEntity in remainingInput.Entities.Keys)
            {
                remainingInput.Entities[predictableEntity] = predictableEntity.Position;
            }
        }
    }

    private static IEntityStateMessage FindStateForEntityId(int entityId, IEntityStateMessage[] authStates)
    {
        foreach (IEntityStateMessage state in authStates) { if (state.EntityId == entityId) { return state; } }
        return null;
    }

    public void DisplayDebugInformation()
    {
        if (ImGui.CollapsingHeader("Snapshot Rollback"))
        {
            ImGui.Text($"Misspredictions: {_misspredictionsCount}");
            ImGui.Text($"Missed Local States: {_missedLocalState}");
            ImGui.Text($"Prediction History: {_predictedStates.Count}");
        }
    }

    private class PredictedState
    {
        public int Tick;                                // Tick at which the input was taken
        public IClientInputData Input;                     // Input message sent to the server
        public Dictionary<IPredictableEntity, Vector3> Entities;
    }
}
