using Godot;

/// <summary>Precisão Sombria: every third basic attack gains bonus damage.</summary>
public partial class NyxaraPassive : Node
{
    [Export] public float BonusDamageMultiplier = 0.8f;

    private CombatComponent _combat;
    private int _attackCount;

    public override void _Ready()
    {
        _combat = GetParent()?.GetNodeOrNull<CombatComponent>("CombatComponent");
        if (_combat != null) _combat.ModifyBasicAttackDamage += ModifyDamage;
    }

    public override void _ExitTree()
    {
        if (_combat != null) _combat.ModifyBasicAttackDamage -= ModifyDamage;
    }

    private float ModifyDamage(HealthComponent target, float damage)
    {
        _attackCount++;
        if (_attackCount < 3) return damage;
        _attackCount = 0;
        return damage + damage * BonusDamageMultiplier;
    }
}
