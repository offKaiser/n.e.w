using Godot;

public partial class SlowAbility : Ability
{
    [Export] public float SlowMultiplier = 0.55f;
    [Export] public float Duration = 2.5f;
    [Export] public float Damage = 15.0f;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        AbyssPassive.DealAbilityDamage(caster, target, Damage * RankMultiplier);
        Node3D unit = target.GetParent<Node3D>();
        float rankSlow = Mathf.Max(0.25f, SlowMultiplier - (Rank - 1) * 0.05f);
        if (unit is EnemyController enemy) enemy.ApplySlow(rankSlow, Duration);
        if (unit is MinionController minion) minion.ApplySlow(rankSlow, Duration);
        TimedVfx vfx = new TimedVfx();
        caster.GetParent().AddChild(vfx);
        vfx.GlobalPosition = unit.GlobalPosition + Vector3.Up;
        vfx.Configure(new Color(0.25f, 0.03f, 0.8f), 1.2f, Duration);
        BeamVfx chains = new BeamVfx();
        caster.GetParent().AddChild(chains);
        chains.Configure(caster.GlobalPosition + Vector3.Up, unit.GlobalPosition + Vector3.Up, new Color(0.43f, 0.08f, 0.95f), 0.09f, 0.38f);
        return true;
    }
}
