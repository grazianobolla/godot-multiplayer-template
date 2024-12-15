using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Client;

public partial class ClientEntityManager : ClientNetworkNode
{
    private EntitySpawner _entitySpawner;

    public override void _EnterTree()
    {
        _entitySpawner = MonkeNetConfig.Instance.EntitySpawner;
    }

    /// <summary>
    /// Requests the server to spawn an entity
    /// </summary>
    /// <param name="entityType"></param>
    public void MakeEntityRequest(byte entityType)
    {
        var req = new EntityRequest
        {
            EntityType = entityType
        };

        SendCommandToServer((byte)MessageTypeEnum.EntityRequest, req, INetworkManager.PacketModeEnum.Reliable, (int)ChannelEnum.EntityEvent);
    }

    protected override void OnCommandReceived(IPackableMessage command)
    {
        if (command is EntityEvent entityEvent)
        {
            switch (entityEvent.Event)
            {
                case EntityEventEnum.Created:
                    _entitySpawner.SpawnEntity(entityEvent);
                    break;
                case EntityEventEnum.Destroyed:
                    _entitySpawner.DestroyEntity(entityEvent);
                    break;
                default:
                    break;
            }
        }
    }
}
