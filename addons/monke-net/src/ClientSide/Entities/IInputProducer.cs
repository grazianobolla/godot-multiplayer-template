using MonkeNet.Shared;

namespace MonkeNet.Client;

public interface IInputProducer
{
    public IClientInputData GetCurrentInput();
}
