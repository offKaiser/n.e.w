using Godot;

public partial class NyxaraGrenadeAbility : Ability
{
    [Export] public float Damage = 18.0f;
    [Export] public float Radius = 2.3f;
    [Export] public float SlowMultiplier = 0.6f;
    [Export] public float SlowDuration = 2.0f;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        Vector3 center = target.GetParent<Node3D>().GlobalPosition;
        ApplyEffect(caster, target);
        foreach (Node node in caster.GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D unit || unit == target.GetParent() || !CombatTeams.IsEnemy(caster, unit)) continue;
            Vector3 offset = unit.GlobalPosition - center;
            offset.Y = 0.0f;
            if (offset.LengthSquared() > Radius * Radius) continue;
            HealthComponent health = unit.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health != null && health.IsAlive) ApplyEffect(caster, health);
        }
        return true;
    }

    private void ApplyEffect(Node3D caster, HealthComponent health)
    {
        health.TakeDamage(Damage * RankMultiplier, caster);
        if (health.GetParent() is IStatusEffectReceiver receiver)
        {
            receiver.ApplySlow(SlowMultiplier, SlowDuration);
        }
    }
}
