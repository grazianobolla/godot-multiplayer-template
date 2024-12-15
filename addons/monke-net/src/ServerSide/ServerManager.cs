using Godot;
using ImGuiNET;
using MonkeNet.NetworkMessages;
using MonkeNet.Serializer;
using MonkeNet.Shared;

namespace MonkeNet.Server;

public partial class ServerManager : Node
{
    [Signal] public delegate void ServerTickEventHandler(int currentTick);
    [Signal] public delegate void ServerNetworkTickEventHandler(int currentTick);
    [Signal] public delegate void ClientConnectedEventHandler(int clientId);
    [Signal] public delegate void ClientDisconnectedEventHandler(int clientId);
    public delegate void CommandReceivedEventHandler(int clientId, IPackableMessage command); // Using a C# signal here because the Godot signal wouldn't accept NetworkMessages.IPackableMessage
    public event CommandReceivedEventHandler CommandReceived;

    public static ServerManager Instance { get; private set; }

    private INetworkManager _networkManager;
    private ServerNetworkClock _serverClock;
    private ServerEntityManager _entityManager;
    private ServerInputReceiver _inputReceiver;

    private int _currentTick = 0;

    public override void _EnterTree()
    {
        Instance = this;

        // Set the _Process() tickrate to be the same as the _PhysicsProcess() to not waste resources, we shouldn't be using _Process() anywhere
        // TODO: Update: Uncommenting this makes the network conditions shit. It seems like maybe it affects packet reading or something like that? Investigate further.
        //Engine.MaxFps = Engine.PhysicsTicksPerSecond; // This should be used
        Engine.MaxFps = 120; // Should be enough...
    }

    public override void _Ready()
    {
        _entityManager = GetNode<ServerEntityManager>("EntityManager");
        _inputReceiver = GetNode<ServerInputReceiver>("InputReceiver");
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
        _currentTick = _serverClock.ProcessTick();

        EmitSignal(SignalName.ServerTick, _currentTick);
        EntitiesCallProcessTick(_currentTick);

        _inputReceiver.DropOutdatedInputs(_currentTick); // Delete all inputs that we don't need anymore

        PhysicsServer3D.Singleton.Call("space_step", MonkeNetManager.Instance.PhysicsSpace, PhysicsUtils.DeltaTime);
        PhysicsServer3D.Singleton.Call("space_flush_queries", MonkeNetManager.Instance.PhysicsSpace);

        _entityManager.SendSnapshotData(_currentTick);
    }

    private void EntitiesCallProcessTick(int currentTick)
    {
        foreach (var node in MonkeNetConfig.Instance.EntitySpawner.Entities)
        {
            if (node is IServerEntity serverEntity)
            {
                IPackableElement input = _inputReceiver.GetInputForEntityTick(serverEntity, currentTick);

                if (input != null)
                {
                    serverEntity.OnProcessTick(currentTick, input);
                }
            }
        }
    }

    private void OnTimerTimeout()
    {
        GD.Print($"Server Status: Tick {_currentTick}, Framerate {Engine.GetFramesPerSecond()}, Physics Tick {Engine.PhysicsTicksPerSecond}hz");
    }

    private void OnNetworkProcess(double delta)
    {
        EmitSignal(SignalName.ServerNetworkTick, _currentTick);
    }

    public void Initialize(INetworkManager networkManager, int port)
    {
        _networkManager = networkManager;

        _serverClock = GetNode<ServerNetworkClock>("ServerClock");
        _serverClock.NetworkProcessTick += OnNetworkProcess;

        _networkManager.CreateServer(port);
        _networkManager.ClientConnected += OnClientConnected;
        _networkManager.ClientDisconnected += OnClientDisconnected;
        _networkManager.PacketReceived += OnPacketReceived;

        GD.Print("Initialized Server Manager");
    }

    public void SendCommandToClient(MessageTypeEnum type, int clientId, IPackableMessage command, INetworkManager.PacketModeEnum mode, int channel)
    {
        byte[] bin = MessageSerializer.Serialize(type, command);
        _networkManager.SendBytes(bin, clientId, channel, mode);
    }

    public int GetNetworkId()
    {
        return _networkManager.GetNetworkId();
    }

    // Route received Input package to the correspondant Network ID
    private void OnPacketReceived(long id, byte[] bin)
    {
        var command = MessageSerializer.Deserialize(bin);
        CommandReceived?.Invoke((int)id, command);
    }

    private void OnClientConnected(long clientId)
    {
        EmitSignal(SignalName.ClientConnected, (int)clientId);
        GD.Print($"Client {clientId} connected");
    }

    private void OnClientDisconnected(long clientId)
    {
        EmitSignal(SignalName.ClientDisconnected, (int)clientId);
        GD.Print($"Client {clientId} disconnected");
    }

    private void DisplayDebugInformation()
    {
        ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
        if (ImGui.Begin("Server Information",
            ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"Framerate {Engine.GetFramesPerSecond()}fps");
            ImGui.Text($"Physics Tick {Engine.PhysicsTicksPerSecond}hz");
            _serverClock.DisplayDebugInformation();
            ImGui.End();
        }

    }
}
