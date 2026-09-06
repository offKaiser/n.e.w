using Godot;

public partial class AbyssFieldAbility : Ability
{
    [Export] public float Damage = 100.0f;
    [Export] public float Radius = 5.0f;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        Vector3 center = target.GetParent<Node3D>().GlobalPosition;
        AbyssField field = new AbyssField();
        caster.GetParent().AddChild(field);
        field.GlobalPosition = center;
        field.Configure(caster, Radius, Damage * RankMultiplier, 5.0f, target);
        return true;
    }
}
