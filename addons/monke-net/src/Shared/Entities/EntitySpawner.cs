using Godot;
using MonkeNet.NetworkMessages;
using System.Collections.Generic;

namespace MonkeNet.Shared;

public abstract partial class EntitySpawner : Node
{
    public static EntitySpawner Instance { get; private set; }

    /// <summary>
    /// Stores a collection of the currently instanced entities
    /// </summary>
    public List<Node> Entities { get; private set; } = [];

    protected abstract Node HandleEntityCreationClientSide(EntityEvent @event);
    protected abstract Node HandleEntityCreationServerSide(EntityEvent @event);

    public override void _Ready()
    {
        Instance = this;
    }

    // Can be called from both the server or a client, so it needs to handle both scenarios
    public Node SpawnEntity(EntityEvent @event)
    {
        Node instancedNode;
        if (MonkeNetManager.Instance.IsServer)
        {
            instancedNode = HandleEntityCreationServerSide(@event);
        }
        else
        {
            instancedNode = HandleEntityCreationClientSide(@event);
        }

        if (instancedNode is not INetworkedEntity networkedEntity)
        {
            throw new MonkeNetException($"Can't spawn entity that is not a {typeof(INetworkedEntity).Name}");
        }

        InitializeEntity(instancedNode, networkedEntity, @event);
        AddChild(instancedNode);
        Entities.Add(instancedNode);
        return instancedNode;
    }

    public void DestroyEntity(EntityEvent @event)
    {
        Node node = GetNode(@event.EntityId.ToString());
        Entities.Remove(node);
        node.Free();
    }

    private static void InitializeEntity(Node node, INetworkedEntity entity, EntityEvent @event)
    {
        node.Name = @event.EntityId.ToString();
        entity.EntityId = @event.EntityId;
        entity.EntityType = @event.EntityType;
        entity.Authority = @event.Authority;
    }
}