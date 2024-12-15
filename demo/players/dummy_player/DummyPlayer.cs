using Godot;
using MonkeNet.Client;
using MonkeNet.Shared;

namespace GameDemo;

// Dummy player (other players in the game)
public partial class DummyPlayer : Node3D, INetworkedEntity, IInterpolatedEntity
{
    [Export] private AnimationTree _animTree;

    public int EntityId { get; set; }
    public byte EntityType { get; set; }
    public int Authority { get; set; }

    // Called by the Snapshot Interpolator every frame, here you solve how to interpolate received states
    public void HandleStateInterpolation(IEntityStateMessage past, IEntityStateMessage future, float interpolationFactor)
    {
        var pastState = (EntityStateMessage)past;
        var futureState = (EntityStateMessage)future;

        // Interpolate position
        this.Position = pastState.Position.Lerp(futureState.Position, interpolationFactor);

        // Interpolate Yaw
        var rotation = Mathf.LerpAngle(pastState.Yaw, futureState.Yaw, interpolationFactor);
        this.Rotation = Vector3.Up * rotation;

        // Interpolate velocity
        Vector3 velocity = pastState.Velocity.Lerp(futureState.Velocity, interpolationFactor);

        UpdateAnimationTree(velocity);
    }

    private void UpdateAnimationTree(Vector3 velocity)
    {
        Vector3 lateralVelocity = velocity * new Vector3(1, 0, 1);

        if (lateralVelocity.IsZeroApprox())
        {
            _animTree.Set("parameters/conditions/idle", true);
            _animTree.Set("parameters/conditions/run", false);
        }
        else
        {
            _animTree.Set("parameters/conditions/idle", false);
            _animTree.Set("parameters/conditions/run", true);
        }
    }
}
