using Godot;

/// <summary>Persistent damage zone created by Nyr'Vela's Domínio Abissal.</summary>
public partial class AbyssField : Node3D
{
    private Node3D _caster;
    private float _radius;
    private float _damagePerTick;
    private float _duration;
    private float _elapsed;
    private float _tickElapsed;
    private HealthComponent _primaryTarget;

    public void Configure(Node3D caster, float radius, float damagePerTick, float duration, HealthComponent primaryTarget = null)
    {
        _caster = caster;
        _radius = radius;
        _damagePerTick = damagePerTick;
        _duration = duration;
        _primaryTarget = primaryTarget;
        AddToGroup("abyss_fields");
        caster.GetNodeOrNull<HealthComponent>("HealthComponent")?.ApplyDamageReduction(0.85f, duration);
        caster.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent")?.ApplyGenerationBoost(1.5f, duration);

        CylinderMesh mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = 0.08f };
        StandardMaterial3D material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.22f, 0.02f, 0.62f, 0.48f),
            EmissionEnabled = true,
            Emission = new Color(0.35f, 0.03f, 0.95f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };
        AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = Vector3.Up * 0.06f });
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        _tickElapsed += (float)delta;
        if (_tickElapsed >= 1.0f)
        {
            _tickElapsed = 0.0f;
            DamageEnemies();
        }
        if (_elapsed >= _duration) QueueFree();
    }

    private void DamageEnemies()
    {
        if (!GodotObject.IsInstanceValid(_caster)) return;
        if (_primaryTarget != null && GodotObject.IsInstanceValid(_primaryTarget) && _primaryTarget.IsAlive)
        {
            Node3D targetOwner = _primaryTarget.GetParent<Node3D>();
            Vector3 targetOffset = targetOwner.GlobalPosition - GlobalPosition;
            targetOffset.Y = 0.0f;
            if (targetOffset.LengthSquared() <= _radius * _radius)
            {
                AbyssPassive.DealAbilityDamage(_caster, _primaryTarget, _damagePerTick);
            }
        }
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D unit || !CombatTeams.IsEnemy(_caster, unit)) continue;
            HealthComponent health = unit.GetNodeOrNull<HealthComponent>("HealthComponent");
            Vector3 offset = unit.GlobalPosition - GlobalPosition;
            offset.Y = 0;
            if (health != null && health != _primaryTarget && health.IsAlive && offset.LengthSquared() <= _radius * _radius)
            {
                AbyssPassive.DealAbilityDamage(_caster, health, _damagePerTick);
                if (unit is EnemyController enemy) enemy.ApplySlow(0.85f, 1.15f);
                if (unit is EnemyController suppressedEnemy) suppressedEnemy.ApplyAbyssSuppression(1.15f, 1.15f);
            }
        }
    }

    public void ExtendIfOwnedBy(Node source)
    {
        for (Node current = source; current != null; current = current.GetParent())
        {
            if (current == _caster)
            {
                _duration += 2.0f;
                _caster.GetNodeOrNull<HealthComponent>("HealthComponent")?.ApplyDamageReduction(0.85f, 2.0f);
                _caster.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent")?.ApplyGenerationBoost(1.5f, 2.0f);
                return;
            }
        }
    }
}
