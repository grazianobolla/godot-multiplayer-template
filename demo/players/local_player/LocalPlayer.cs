using Godot;
using MonkeNet;
using MonkeNet.Client;
using MonkeNet.Shared;

namespace GameDemo;

public partial class LocalPlayer : CharacterBody3D, IPredictableEntity, IInputProducer
{
    [Export] private FirstPersonCameraController _cameraController;
    [Export] private float _maxDeviationAllowedSquared = 0.001f;
    [Export] private SharedPlayerMovement _playerMovement;

    public int EntityId { get; set; }
    public byte EntityType { get; set; }
    public int Authority { get; set; }

    public override void _Ready()
    {
        // Set this node as the input producer, this means that MonkeNet's client manager will
        // use this nodes GetCurrenInput() method to generate inputs for the server
        MonkeNetConfig.Instance.InputProducer = this;
    }

    // Called every physics tick (but synced to network clock)
    public void OnProcessTick(int tick, int remoteTick, IClientInputData genericInput)
    {
        CharacterInputMessage input = (CharacterInputMessage)genericInput;
        _playerMovement.AdvancePhysics(input);
    }

    // Generates input message client will send to server
    public IClientInputData GetCurrentInput()
    {
        return new CharacterInputMessage
        {
            Keys = GetCurrentPressedKeys(),
            CameraYaw = _cameraController.GetLateralRotationAngle()
        };
    }

    // We have misspredicted, return player back to authoritative position
    public void HandleReconciliation(IEntityStateMessage receivedState)
    {
        EntityStateMessage state = (EntityStateMessage)receivedState;
        this.Position = state.Position;
        this.Velocity = state.Velocity;
    }

    // Check if we have misspredicted
    public bool HasMisspredicted(IEntityStateMessage receivedState, Vector3 savedPosition)
    {
        EntityStateMessage state = (EntityStateMessage)receivedState;
        return (state.Position - savedPosition).LengthSquared() > _maxDeviationAllowedSquared;
    }

    // When the client is re-simulating inputs, what should we do with it? usually the same we do on process tick
    public void ResimulateTick(IClientInputData input)
    {
        _playerMovement.AdvancePhysics((CharacterInputMessage)input);
    }

    private static byte GetCurrentPressedKeys()
    {
        byte keys = 0;
        if (Input.IsActionPressed("right")) keys |= (byte)InputFlags.Right;
        if (Input.IsActionPressed("left")) keys |= (byte)InputFlags.Left;
        if (Input.IsActionPressed("forward")) keys |= (byte)InputFlags.Forward;
        if (Input.IsActionPressed("backward")) keys |= (byte)InputFlags.Backward;
        if (Input.IsActionPressed("space")) keys |= (byte)InputFlags.Space;
        if (Input.IsActionPressed("shift")) keys |= (byte)InputFlags.Shift;
        return keys;
    }
}
