using Godot;
using MonkeNet.Shared;

namespace MonkeNet.Client;

public interface IPredictableEntity : IClientEntity
{
    public Vector3 Position { get; set; }
    public bool HasMisspredicted(IEntityStateMessage receivedState, Vector3 savedState);
    public void HandleReconciliation(IEntityStateMessage receivedState);
    public void ResimulateTick(IClientInputData input);
}
