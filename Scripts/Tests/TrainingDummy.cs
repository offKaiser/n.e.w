using Godot;

/// <summary>Minimal core-test target that records generic status effects.</summary>
public partial class TrainingDummy : StaticBody3D, IStatusEffectReceiver, ITeamMember
{
    [Export] public TeamId Team = TeamId.Red;
    TeamId ITeamMember.TeamId => Team;
    public float SlowMultiplier { get; private set; } = 1.0f;
    public double SlowEndsAt { get; private set; }

    public void ApplySlow(float multiplier, float duration)
    {
        SlowMultiplier = Mathf.Min(SlowMultiplier, multiplier);
        SlowEndsAt = Mathf.Max(SlowEndsAt, Time.GetTicksMsec() / 1000.0 + duration);
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        // A stationary dummy accepts the interface without gaining movement.
    }

    public override void _Process(double delta)
    {
        if (Time.GetTicksMsec() / 1000.0 >= SlowEndsAt)
        {
            SlowMultiplier = 1.0f;
        }
    }
}
