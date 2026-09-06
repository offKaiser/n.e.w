using Godot;

public partial class CombatComponent : Node
{
    [Export]
    public float AttackDamage = 20.0f;

    [Export]
    public float AttackRange = 3.0f;

    [Export]
    public float AttackCooldown = 0.75f;

    private HealthComponent _target;
    private double _nextAttackTime;

    public bool HasValidTarget => _target != null && GodotObject.IsInstanceValid(_target) && _target.IsAlive;
    public HealthComponent CurrentTarget => HasValidTarget ? _target : null;
    public Vector3 TargetPosition => _target.GetParent<Node3D>().GlobalPosition;

    public void SetTarget(HealthComponent target)
    {
        _target = target;
    }

    public void ClearTarget()
    {
        _target = null;
    }

    public void TryAttack(Vector3 attackerPosition)
    {
        if (!HasValidTarget)
        {
            ClearTarget();
            return;
        }

        Vector3 offset = TargetPosition - attackerPosition;
        offset.Y = 0.0f;
        if (offset.LengthSquared() > AttackRange * AttackRange)
        {
            return;
        }

        double currentTime = Time.GetTicksMsec() / 1000.0;
        if (currentTime < _nextAttackTime)
        {
            return;
        }

        Node3D caster = GetParent<Node3D>();
        AbyssPassive.DealBasicDamage(caster, _target, AttackDamage);
        _nextAttackTime = currentTime + AttackCooldown;
    }
}
