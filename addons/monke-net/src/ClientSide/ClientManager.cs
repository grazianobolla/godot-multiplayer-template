using Godot;
using ImGuiNET;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Client;

/// <summary>
/// Main Client-side node, communicates with the server and other components of the client
/// </summary>
public partial class ClientManager : Node
{
    [Signal] public delegate void ClientTickEventHandler(int currentTick, int currentRemoteTick);
    [Signal] public delegate void LatencyCalculatedEventHandler(int latencyAverageTicks, int jitterAverageTicks);
    [Signal] public delegate void NetworkReadyEventHandler();

    public delegate void CommandReceivedEventHandler(IPackableMessage command); // Using a C# signal here because the Godot signal wouldn't accept NetworkMessages.IPackableMessage
    public event CommandReceivedEventHandler CommandReceived;

    public static ClientManager Instance { get; private set; }

    private INetworkManager _networkManager;
    private SnapshotInterpolator _snapshotInterpolator;
    private ClientNetworkClock _clock;
    private NetworkDebug _networkDebug;
    private ClientEntityManager _entityManager;
    private ClientInputManager _inputManager;
    private SnapshotRollbacker _snapshotRollbacker;

    private bool _networkReady = false;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        _networkDebug = GetNode<NetworkDebug>("NetworkDebug");
        _clock = GetNode<ClientNetworkClock>("ClientClock");
        _snapshotInterpolator = GetNode<SnapshotInterpolator>("SnapshotInterpolator");
        _entityManager = GetNode<ClientEntityManager>("EntityManager");
        _inputManager = GetNode<ClientInputManager>("InputManager");
        _snapshotRollbacker = GetNode<SnapshotRollbacker>("SnapshotRollbacker");
    }

    public override void _Process(double delta)
    {
        DisplayDebugInformation();
    }

    // TODO: I don't know if manually stepping physics inside _PhysicsProcess is a good idea,
    // as internally _PhysicsProcess will call _step() and _flush_queries() the same way I'm doing right now...
    // causing multiple calls to the same PhysicsServer methods
    public override void _PhysicsProcess(double delta)
    {
        // Advance Clock
        _clock.ProcessTick();
        int currentTick = _clock.GetCurrentTick();
        int currentRemoteTick = _clock.GetCurrentRemoteTick();

        var input = _inputManager.GenerateAndTransmitInputs(currentRemoteTick);         // Read and send produced input to the server
        EntitiesCallProcessTick(currentTick, currentRemoteTick, input);                                    // Call OnProcessTick on all entities, pass current input so they can simulate
        EmitSignal(SignalName.ClientTick, currentTick, currentRemoteTick);

        PhysicsServer3D.Singleton.Call("space_step", MonkeNetManager.Instance.PhysicsSpace, PhysicsUtils.DeltaTime);
        PhysicsServer3D.Singleton.Call("space_flush_queries", MonkeNetManager.Instance.PhysicsSpace);

        _snapshotRollbacker.RegisterPrediction(currentRemoteTick, input);               // Register all local predictions
    }

    // Calls OnProcessTick on all entities
    private static void EntitiesCallProcessTick(int currentTick, int remoteTick, IClientInputData input)
    {
        foreach (var node in MonkeNetConfig.Instance.EntitySpawner.Entities)
        {
            if (node is IClientEntity clientEntity)
            {
                clientEntity.OnProcessTick(currentTick, remoteTick, input);
            }
        }
    }

    public void Initialize(INetworkManager networkManager, string address, int port)
    {
        _networkManager = networkManager;
        _networkDebug.NetworkManager = _networkManager;

        _clock.LatencyCalculated += OnLatencyCalculated;

        _networkManager.PacketReceived += OnPacketReceived;
        _networkManager.CreateClient(address, port);

        GD.Print("Client Manager Initialized");
    }

    public void SendCommandToServer(MessageTypeEnum type, IPackableMessage command, INetworkManager.PacketModeEnum mode, int channel)
    {
        byte[] bin = MessageSerializer.Serialize(type, command);
        _networkManager.SendBytes(bin, 1, channel, mode);
    }

    private void OnPacketReceived(long id, byte[] bin)
    {
        var command = MessageSerializer.Deserialize(bin);
        CommandReceived?.Invoke(command);
    }

    public void MakeEntityRequest(byte entityType) //TODO: This should NOT be here
    {
        _entityManager.MakeEntityRequest(entityType);
    }

    public int GetNetworkId()
    {
        return _networkManager.GetNetworkId();
    }

    private void OnLatencyCalculated(int latencyAverageTicks, int jitterAverageTicks)
    {
        EmitSignal(SignalName.LatencyCalculated, latencyAverageTicks, jitterAverageTicks);
        EmitSignal(SignalName.NetworkReady); //TODO: calculate this in other way, this should only be emmited once and
                                             //right now it will be emitted every time the colck calculates latency
        _networkReady = true;
    }

    private void DisplayDebugInformation()
    {
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
        if (ImGui.Begin("Client Information",
                ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"Network ID {Multiplayer.GetUniqueId()}");
            ImGui.Text($"Framerate {Engine.GetFramesPerSecond()}fps");
            ImGui.Text($"Physics Tick {Engine.PhysicsTicksPerSecond}hz");
            _clock.DisplayDebugInformation();
            _networkDebug.DisplayDebugInformation();
            _snapshotInterpolator.DisplayDebugInformation();
            _inputManager.DisplayDebugInformation();
            _snapshotRollbacker.DisplayDebugInformation();
            ImGui.End();
        }
    }
}
