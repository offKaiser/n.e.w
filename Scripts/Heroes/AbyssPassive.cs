using Godot;

public static class AbyssPassive
{
    public static void DealAbilityDamage(Node3D caster, HealthComponent target, float damage)
    {
        AbyssEnergyComponent passive = caster.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent");
        float bonus = passive?.ConsumeEmpowerment() ?? 0.0f;
        target.TakeDamage(damage + bonus, caster);
        ApplyMark(caster, target, bonus);
        passive?.GainFromAbility();
    }

    public static void DealBasicDamage(Node3D caster, HealthComponent target, float damage)
    {
        AbyssEnergyComponent passive = caster.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent");
        float bonus = passive?.ConsumeEmpowerment() ?? 0.0f;
        target.TakeDamage(damage + bonus, caster);
        ApplyMark(caster, target, bonus);
    }

    private static void ApplyMark(Node3D caster, HealthComponent target, float bonus)
    {
        if (bonus <= 0.0f) return;
        Node owner = target.GetParent();
        AbyssMark mark = owner.GetNodeOrNull<AbyssMark>("AbyssMark");
        if (mark == null)
        {
            mark = new AbyssMark { Name = "AbyssMark" };
            owner.AddChild(mark);
        }
        mark.Apply(caster, 3.0f);
    }
}
