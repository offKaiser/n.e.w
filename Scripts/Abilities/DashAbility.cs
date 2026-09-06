using Godot;

public partial class DashAbility : Ability
{
    [Export] public float DashDistance = 5.0f;
    [Export] public float Damage = 45.0f;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        Vector3 shadowPosition = caster.GlobalPosition;
        DelayedAbyssExplosion shadow = new DelayedAbyssExplosion();
        caster.GetParent().AddChild(shadow);
        shadow.GlobalPosition = shadowPosition;
        shadow.Configure(caster, 2.8f, Damage * RankMultiplier, 1.5f);
        Vector3 forward = -caster.GlobalTransform.Basis.Z;
        caster.GlobalPosition += forward * DashDistance;
        TimedVfx vfx = new TimedVfx();
        caster.GetParent().AddChild(vfx);
        vfx.GlobalPosition = caster.GlobalPosition + Vector3.Up * 0.5f;
        vfx.Configure(new Color(0.4f, 0.04f, 0.9f), 2.0f, 0.6f);
        return true;
    }
}
