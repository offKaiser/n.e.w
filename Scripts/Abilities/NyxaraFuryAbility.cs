using Godot;

public partial class NyxaraFuryAbility : Ability
{
    [Export] public float AttackSpeedMultiplier = 1.6f;
    [Export] public float Duration = 4.0f;

    public NyxaraFuryAbility()
    {
        TargetType = AbilityTargetType.Self;
    }

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        CombatComponent combat = caster.GetNodeOrNull<CombatComponent>("CombatComponent");
        if (combat == null) return false;
        combat.ActivateAttackSpeedBoost(AttackSpeedMultiplier, Duration);
        return true;
    }
}
