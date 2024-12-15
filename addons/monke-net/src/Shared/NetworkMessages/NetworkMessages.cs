using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.NetworkMessages;

public enum EntityEventEnum : byte //TODO: move somewhere else
{
    Created,
    Destroyed

}
public enum ChannelEnum : int
{
    Snapshot,
    Clock,
    EntityEvent,
    ClientInput
}

public enum MessageTypeEnum : byte
{
    EntityRequest,
    ClockSync,
    EntityEvent,
    EntityState,
    GameSnapshot,
    PackedClientInputs,
    ClientInputData,
    ButtonData // TODO: wtf why do I have to define this here?
}

[RegisterMessage(MessageTypeEnum.EntityRequest, typeof(EntityRequest))]
public struct EntityRequest : IPackableMessage
{
    public required byte EntityType { get; set; }

    public void ReadBytes(MessageReader reader)
    {
        EntityType = reader.ReadByte();
    }

    public readonly void WriteBytes(MessageWriter writer)
    {
        writer.Write(EntityType);
    }
}

[RegisterMessage(MessageTypeEnum.ClockSync, typeof(ClockSync))]
public struct ClockSync : IPackableMessage
{
    public required int ClientTime { get; set; }
    public required int ServerTime { get; set; }

    public void ReadBytes(MessageReader reader)
    {
        ClientTime = reader.ReadInt32();
        ServerTime = reader.ReadInt32();
    }

    public readonly void WriteBytes(MessageWriter writer)
    {
        writer.Write(ClientTime);
        writer.Write(ServerTime);
    }
}

[RegisterMessage(MessageTypeEnum.EntityEvent, typeof(EntityEvent))]
public struct EntityEvent : IPackableMessage
{
    public required EntityEventEnum Event { get; set; }
    public required int EntityId { get; set; }
    public required byte EntityType { get; set; }
    public required int Authority { get; set; }

    public void ReadBytes(MessageReader reader)
    {
        Event = (EntityEventEnum)reader.ReadByte();
        EntityId = reader.ReadInt32();
        EntityType = reader.ReadByte();
        Authority = reader.ReadInt32();
    }

    public readonly void WriteBytes(MessageWriter writer)
    {
        writer.Write((byte)Event);
        writer.Write(EntityId);
        writer.Write(EntityType);
        writer.Write(Authority);
    }

}

[RegisterMessage(MessageTypeEnum.GameSnapshot, typeof(GameSnapshot))]
public struct GameSnapshot : IPackableMessage
{
    public required int Tick { get; set; }
    public IEntityStateMessage[] States { get; set; }

    public readonly void WriteBytes(MessageWriter writer)
    {
        writer.Write(Tick);
        writer.Write(States);
    }

    public void ReadBytes(MessageReader reader)
    {
        Tick = reader.ReadInt32();
        States = reader.ReadArray<IEntityStateMessage>(MessageTypeEnum.EntityState);
    }
}

[RegisterMessage(MessageTypeEnum.PackedClientInputs, typeof(PackedClientInputs))]
public struct PackedClientInputs : IPackableMessage
{
    public required int Tick { get; set; } // This is the Tick stamp for the latest generated input (Inputs[Inputs.Length]), all other Ticks are (Tick - index)
    public IPackableElement[] Inputs { get; set; }

    public readonly void WriteBytes(MessageWriter writer)
    {
        writer.Write(Tick);
        writer.Write(Inputs);
    }

    public void ReadBytes(MessageReader reader)
    {
        Tick = reader.ReadInt32();
        Inputs = reader.ReadArray<IPackableElement>(MessageTypeEnum.ClientInputData);
    }
}