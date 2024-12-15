using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Server;

/// <summary>
/// Implement this on your server entity, the server entity manager will pick up all IServerEntity and broadcasts their states to clients
/// </summary>
public interface IServerEntity : INetworkedEntity
{
    public abstract void OnProcessTick(int tick, IPackableElement input);
    public IEntityStateMessage GenerateCurrentStateMessage();
}