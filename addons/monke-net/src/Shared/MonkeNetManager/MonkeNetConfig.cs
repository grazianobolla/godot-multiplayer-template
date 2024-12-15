using Godot;
using MonkeNet.Client;
using MonkeNet.Shared;

namespace MonkeNet;

/// <summary>
/// Main MonkeNet configuration singleton.
/// </summary>
[GlobalClass, Icon("res://addons/monke-net/resources/circle_nodes_solid.png")]
public partial class MonkeNetConfig : Node
{
    public static MonkeNetConfig Instance { get; set; }

    /// <summary>
    /// Controls how different entities are spawned on both the client and server.
    /// </summary>
    [Export] public EntitySpawner EntitySpawner { get; set; }

    /// <summary>
    /// Stores a reference to the local input producer when running on the client client.
    /// </summary>
    public IInputProducer InputProducer { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
    }
}
