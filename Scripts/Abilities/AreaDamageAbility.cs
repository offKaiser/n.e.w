using Godot;

public partial class AreaDamageAbility : Ability
{
    [Export] public float Damage = 40.0f;
    [Export] public float Radius = 3.0f;
    [Export] public Color VfxColor = new Color(0.35f, 0.05f, 0.9f);
    [Export] public bool EnableLegacyVfx = true;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        Vector3 center = target.GetParent<Node3D>().GlobalPosition;
        // The selected target is always affected. Additional targets still use
        // the legacy team query until team affiliation is refactored.
        AbyssPassive.DealAbilityDamage(caster, target, Damage * RankMultiplier);
        foreach (Node node in caster.GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D unit || !CombatTeams.IsEnemy(caster, unit)) continue;
            HealthComponent health = unit.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health == null || health == target || !health.IsAlive) continue;
            Vector3 offset = unit.GlobalPosition - center;
            offset.Y = 0.0f;
            if (offset.LengthSquared() <= Radius * Radius) AbyssPassive.DealAbilityDamage(caster, health, Damage * RankMultiplier);
        }

        if (EnableLegacyVfx)
        {
            TimedVfx vfx = new TimedVfx();
            caster.GetParent().AddChild(vfx);
            vfx.GlobalPosition = center + Vector3.Up * 0.4f;
            vfx.Configure(VfxColor, Radius, 0.5f);
        }
        return true;
    }
}
