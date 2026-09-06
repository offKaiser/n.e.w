using Godot;

public partial class SpeedBoostAbility : Ability
{
    [Export]
    public float SpeedMultiplier = 1.6f;

    [Export]
    public float Duration = 3.0f;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        if (caster is not IStatusEffectReceiver statusReceiver)
        {
            return false;
        }

        statusReceiver.ActivateSpeedBoost(SpeedMultiplier, Duration);
        TimedVfx vfx = new TimedVfx();
        caster.GetParent().AddChild(vfx);
        vfx.GlobalPosition = caster.GlobalPosition + Vector3.Up;
        vfx.Configure(new Color(0.32f, 0.08f, 0.9f), 1.4f, 0.5f);
        return true;
    }
}
