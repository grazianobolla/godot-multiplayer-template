using Godot;
using ImGuiNET;
using MonkeNet.Shared;

namespace GameDemo;

/// <summary>
/// Shared player movement code, used to move both client and server players.
/// </summary>
public partial class SharedPlayerMovement : Node
{
    [Export] private CharacterBody3D _characterBody;

    public void AdvancePhysics(CharacterInputMessage input)
    {
        _characterBody.Velocity = PlayerMovementCalculator.CalculateVelocity(_characterBody, input);
        PhysicsUtils.MoveAndSlide(_characterBody);
    }

    public void DisplayDebugInformation()
    {
        if (ImGui.CollapsingHeader("Movement"))
        {
            ImGui.Text($"Position ({_characterBody.GlobalPosition.X:0.00}, {_characterBody.GlobalPosition.Y:0.00}, {_characterBody.GlobalPosition.Z:0.00})");
            ImGui.Text($"Velocity ({_characterBody.Velocity.X:0.00}, {_characterBody.Velocity.Y:0.00}, {_characterBody.Velocity.Z:0.00})");
        }
    }
}
