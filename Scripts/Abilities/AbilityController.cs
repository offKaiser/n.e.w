using System;
using Godot;

public enum AbilitySlot
{
    Q,
    W,
    E,
    R
}

/// <summary>
/// Coordinates ability slots, resource validation and casts for one hero.
/// Effects remain inside the concrete Ability classes.
/// </summary>
public partial class AbilityController : Node
{
    public event Action<AbilitySlot, Ability, HealthComponent> AbilityCast;
    public event Action<AbilitySlot> AbilityCastRejected;

    private Node3D _caster;
    private ManaComponent _mana;
    private Ability _abilityQ;
    private Ability _abilityW;
    private Ability _abilityE;
    private Ability _abilityR;

    public override void _Ready()
    {
        _caster = GetParent() as Node3D;
        if (_caster == null)
        {
            GD.PushError($"{nameof(AbilityController)} must be a child of Node3D.");
            return;
        }

        _mana = _caster.GetNodeOrNull<ManaComponent>("ManaComponent");
        _abilityQ = _caster.GetNodeOrNull<Ability>("AbilityQ");
        _abilityW = _caster.GetNodeOrNull<Ability>("AbilityW");
        _abilityE = _caster.GetNodeOrNull<Ability>("AbilityE");
        _abilityR = _caster.GetNodeOrNull<Ability>("AbilityR");
    }

    public Ability GetAbility(AbilitySlot slot) => slot switch
    {
        AbilitySlot.Q => _abilityQ,
        AbilitySlot.W => _abilityW,
        AbilitySlot.E => _abilityE,
        AbilitySlot.R => _abilityR,
        _ => null
    };

    public Ability GetAbility(string slot) => Enum.TryParse(slot, true, out AbilitySlot parsed) ? GetAbility(parsed) : null;

    public bool TryCast(AbilitySlot slot, HealthComponent target)
    {
        Ability ability = GetAbility(slot);
        if (_caster == null || ability == null || !ability.TryCast(_caster, target, _mana))
        {
            AbilityCastRejected?.Invoke(slot);
            return false;
        }

        AbilityCast?.Invoke(slot, ability, target);
        return true;
    }

    public bool TryCast(string slot, HealthComponent target)
    {
        return Enum.TryParse(slot, true, out AbilitySlot parsed) && TryCast(parsed, target);
    }

    public bool TryIncreaseRank(string slot, ProgressionComponent progression)
    {
        return GetAbility(slot)?.TryIncreaseRank(progression) ?? false;
    }
}
