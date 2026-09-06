using Godot;

public partial class DamageAbility : Ability
{
    [Export]
    public float Damage = 35.0f;

    [Export]
    public Color VfxColor = new Color(0.45f, 0.1f, 1.0f);

    [Export]
    public float VfxRadius = 1.0f;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        target.TakeDamage(Damage, caster);
        TimedVfx vfx = new TimedVfx();
        caster.GetParent().AddChild(vfx);
        vfx.GlobalPosition = target.GetParent<Node3D>().GlobalPosition + Vector3.Up;
        vfx.Configure(VfxColor, VfxRadius, 0.35f);
        return true;
    }
}
