using System;
using Godot;

/// <summary>Nyr'Vela passive: ability damage stores Abyss Energy; 100 energy empowers the next hit.</summary>
public partial class AbyssEnergyComponent : Node
{
    [Export] public float MaximumEnergy = 100.0f;
    [Export] public float BonusMagicDamage = 28.0f;
    public float Energy { get; private set; }
    public bool Empowered => Energy >= MaximumEnergy;
    public event Action<float, float> EnergyChanged;
    private float _generationMultiplier = 1.0f;
    private double _generationBoostEndTime;

    public override void _Ready()
    {
        CallDeferred(nameof(ConnectGameplayEvents));
    }

    public override void _ExitTree()
    {
        CombatComponent combat = GetParent()?.GetNodeOrNull<CombatComponent>("CombatComponent");
        AbilityController abilities = GetParent()?.GetNodeOrNull<AbilityController>("AbilityController");
        if (combat != null) combat.ModifyBasicAttackDamage -= ModifyBasicAttackDamage;
        if (abilities != null) abilities.AbilityCast -= OnAbilityCast;
    }

    public void GainFromAbility()
    {
        if (Time.GetTicksMsec() / 1000.0 >= _generationBoostEndTime) _generationMultiplier = 1.0f;
        Energy = Mathf.Min(MaximumEnergy, Energy + 20.0f * _generationMultiplier);
        NotifyEnergyChanged();
    }

    public float ConsumeEmpowerment()
    {
        if (!Empowered) return 0.0f;
        Energy = 0.0f;
        NotifyEnergyChanged();
        return BonusMagicDamage;
    }

    public void SynchronizeEnergy(float energy)
    {
        Energy = Mathf.Clamp(energy, 0.0f, MaximumEnergy);
        NotifyEnergyChanged();
    }

    public void ApplyGenerationBoost(float multiplier, float duration)
    {
        _generationMultiplier = Mathf.Max(_generationMultiplier, multiplier);
        _generationBoostEndTime = Mathf.Max(_generationBoostEndTime, Time.GetTicksMsec() / 1000.0 + duration);
    }

    public float ModifyBasicAttackDamage(HealthComponent target, float damage)
    {
        float bonus = ConsumeEmpowerment();
        if (bonus > 0.0f && GetParent() is Node3D caster)
        {
            AbyssPassive.ApplyMark(caster, target, 3.0f);
        }
        return damage + bonus;
    }

    private void ConnectGameplayEvents()
    {
        Node owner = GetParent();
        CombatComponent combat = owner?.GetNodeOrNull<CombatComponent>("CombatComponent");
        AbilityController abilities = owner?.GetNodeOrNull<AbilityController>("AbilityController");
        if (combat != null) combat.ModifyBasicAttackDamage += ModifyBasicAttackDamage;
        if (abilities != null) abilities.AbilityCast += OnAbilityCast;
    }

    private void OnAbilityCast(AbilitySlot slot, Ability ability, HealthComponent target)
    {
        GainFromAbility();
    }

    private void NotifyEnergyChanged()
    {
        EnergyChanged?.Invoke(Energy, MaximumEnergy);
    }
}
