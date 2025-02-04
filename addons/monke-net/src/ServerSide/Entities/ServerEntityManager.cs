﻿using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Server;

/// <summary>
/// Handles creation/deletion of entities
/// </summary>
public partial class ServerEntityManager : ServerNetworkNode
{
    private EntitySpawner _entitySpawner;
    private int _entityIdCount = 0;
    public override void _EnterTree()
    {
        _entitySpawner = MonkeNetConfig.Instance.EntitySpawner;
    }

    public void SendSnapshotData(int currentTick)
    {
        var snapshotCommand = PackSnapshot(currentTick);
        SendCommandToClient(MessageTypeEnum.GameSnapshot, 0, snapshotCommand, INetworkManager.PacketModeEnum.Unreliable, (int)ChannelEnum.Snapshot);
    }

    protected override void OnCommandReceived(int clientId, IPackableMessage command)
    {
        if (command is EntityRequest entityRequest)
        {
            SpawnEntity(++_entityIdCount, entityRequest.EntityType, (int)NetworkManagerEnet.AudienceMode.Broadcast, clientId);
        }
    }

    protected override void OnClientConnected(int peerId)
    {
        SendWorldState(peerId);
    }

    protected override void OnClientDisconnected(int peerId)
    {
        DestroyEntity(peerId, (int)NetworkManagerEnet.AudienceMode.Broadcast); //FIXME: remove all entities associated with the peerId?
    }

    /// <summary>
    /// Packs the current game state for a tick (Snapshot)
    /// </summary>
    /// <param name="currentTick"></param>
    private IPackableMessage PackSnapshot(int currentTick)
    {
        var entityCount = _entitySpawner.Entities.Count;

        var snapshot = new GameSnapshot
        {
            Tick = currentTick,
            States = new IEntityStateMessage[entityCount]
        };

        for (int i = 0; i < entityCount; i++)
        {
            var networkedEntity = (IServerEntity)_entitySpawner.Entities[i];
            snapshot.States[i] = networkedEntity.GenerateCurrentStateMessage();
        }

        return snapshot;
    }

    /// <summary>
    /// Notifies all clients that an Entity has spawned
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="entityType"></param>
    /// <param name="targetId"></param>
    /// <param name="authority"></param>
    private void SpawnEntity(int entityId, byte entityType, int targetId, int authority)
    {
        var entityEvent = new EntityEvent
        {
            Event = EntityEventEnum.Created,
            EntityId = entityId,
            EntityType = entityType,
            Authority = authority
        };

        _entitySpawner.SpawnEntity(entityEvent); // Execute event locally

        SendCommandToClient(MessageTypeEnum.EntityEvent, targetId, entityEvent, INetworkManager.PacketModeEnum.Reliable, (int)ChannelEnum.EntityEvent);
    }

    /// <summary>
    /// Notifies all clients that an Entity has been destroyed
    /// </summary>
    /// <param name="entityId"></param>
    /// <param name="targetId"></param>
    private void DestroyEntity(int entityId, int targetId)
    {
        var entityEvent = new EntityEvent
        {
            Event = EntityEventEnum.Destroyed,
            EntityId = entityId,
            EntityType = 0,
            Authority = 0
        };

        _entitySpawner.DestroyEntity(entityEvent);  // Execute event locally

        SendCommandToClient(MessageTypeEnum.EntityEvent, targetId, entityEvent, INetworkManager.PacketModeEnum.Reliable, (int)ChannelEnum.EntityEvent);
    }

    /// <summary>
    /// Sends the whole game state to a specific clientId, used when the client connects to replicate world state
    /// </summary>
    /// <param name="clientId"></param>
    private void SendWorldState(int clientId)
    {
        foreach (INetworkedEntity entity in _entitySpawner.GetChildren())
        {
            var entityEvent = new EntityEvent
            {
                Event = EntityEventEnum.Created,
                EntityId = entity.EntityId,
                EntityType = entity.EntityType,
                Authority = entity.Authority,
            };

            SendCommandToClient(MessageTypeEnum.EntityEvent, clientId, entityEvent, INetworkManager.PacketModeEnum.Reliable, (int)ChannelEnum.EntityEvent);
        }

    }
}