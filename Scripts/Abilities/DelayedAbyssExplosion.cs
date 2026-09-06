using Godot;

/// <summary>The shadow left behind by Passo Sombrio, detonating after a delay.</summary>
public partial class DelayedAbyssExplosion : Node3D
{
    private Node3D _caster;
    private float _radius;
    private float _damage;
    private float _remaining;

    public void Configure(Node3D caster, float radius, float damage, float delay)
    {
        _caster = caster;
        _radius = radius;
        _damage = damage;
        _remaining = delay;
        CylinderMesh mesh = new CylinderMesh { TopRadius = radius * 0.35f, BottomRadius = radius * 0.35f, Height = 0.05f };
        StandardMaterial3D material = new StandardMaterial3D { AlbedoColor = new Color(0.13f, 0.01f, 0.32f, 0.7f), EmissionEnabled = true, Emission = new Color(0.3f, 0.02f, 0.7f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = Vector3.Up * 0.05f });
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining > 0) return;
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D unit || !CombatTeams.IsEnemy(_caster, unit)) continue;
            HealthComponent health = unit.GetNodeOrNull<HealthComponent>("HealthComponent");
            Vector3 offset = unit.GlobalPosition - GlobalPosition; offset.Y = 0;
            if (health != null && health.IsAlive && offset.LengthSquared() <= _radius * _radius) AbyssPassive.DealAbilityDamage(_caster, health, _damage);
        }
        TimedVfx burst = new TimedVfx(); GetParent().AddChild(burst);
        burst.GlobalPosition = GlobalPosition + Vector3.Up * 0.3f;
        burst.Configure(new Color(0.47f, 0.05f, 1.0f), _radius, 0.5f);
        QueueFree();
    }
}
