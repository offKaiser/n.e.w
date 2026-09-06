using Godot;
using System;

/// <summary>
/// Owns basic attack state: target, range, damage and attack cooldown.
/// Controllers decide when to request movement; this component only reports
/// whether a target is in range and applies a valid attack.
/// </summary>
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
    private float _attackSpeedMultiplier = 1.0f;
    private double _attackSpeedBoostEndTime;

    public float AttackInterval => AttackCooldown;
    public float CurrentAttackInterval => AttackCooldown / GetAttackSpeedMultiplier();
    public event Func<HealthComponent, float, float> ModifyBasicAttackDamage;
    public event Func<HealthComponent, float, Node, bool> AttackDelivery;
    public bool HasValidTarget => _target != null && GodotObject.IsInstanceValid(_target) && _target.IsAlive;
    public HealthComponent CurrentTarget => HasValidTarget ? _target : null;

    public void SetTarget(HealthComponent target)
    {
        if (_target == target)
        {
            return;
        }

        UnsubscribeFromTarget();
        _target = target != null && target.IsAlive ? target : null;
        if (_target != null)
        {
            _target.Died += OnTargetDied;
        }
    }

    public void ClearTarget()
    {
        UnsubscribeFromTarget();
        _target = null;
    }

    public bool IsTargetInRange(Vector3 attackerPosition)
    {
        if (!TryGetTargetPosition(out Vector3 targetPosition))
        {
            ClearTarget();
            return false;
        }

        Vector3 offset = targetPosition - attackerPosition;
        offset.Y = 0.0f;
        return offset.LengthSquared() <= AttackRange * AttackRange;
    }

    public Vector3 GetApproachDirection(Vector3 attackerPosition)
    {
        if (!TryGetTargetPosition(out Vector3 targetPosition))
        {
            ClearTarget();
            return Vector3.Zero;
        }

        Vector3 offset = targetPosition - attackerPosition;
        offset.Y = 0.0f;
        return offset.LengthSquared() <= AttackRange * AttackRange ? Vector3.Zero : offset.Normalized();
    }

    public bool TryAttack(Vector3 attackerPosition)
    {
        if (!IsTargetInRange(attackerPosition))
        {
            return false;
        }

        double currentTime = Time.GetTicksMsec() / 1000.0;
        if (currentTime < _nextAttackTime)
        {
            return false;
        }

        float damage = ResolveModifiedDamage(_target, AttackDamage);
        Node source = GetParent();
        bool delivered = AttackDelivery?.Invoke(_target, damage, source) ?? false;
        if (!delivered) _target.TakeDamage(damage, source);

        _nextAttackTime = currentTime + CurrentAttackInterval;
        return true;
    }

    public override void _ExitTree()
    {
        UnsubscribeFromTarget();
    }

    public void ActivateAttackSpeedBoost(float multiplier, float duration)
    {
        _attackSpeedMultiplier = Mathf.Max(_attackSpeedMultiplier, multiplier);
        _attackSpeedBoostEndTime = Mathf.Max(_attackSpeedBoostEndTime, Time.GetTicksMsec() / 1000.0 + duration);
    }

    private float GetAttackSpeedMultiplier()
    {
        if (Time.GetTicksMsec() / 1000.0 >= _attackSpeedBoostEndTime) _attackSpeedMultiplier = 1.0f;
        return _attackSpeedMultiplier;
    }

    private bool TryGetTargetPosition(out Vector3 targetPosition)
    {
        targetPosition = Vector3.Zero;
        if (!HasValidTarget || _target.GetParent() is not Node3D targetOwner)
        {
            return false;
        }

        targetPosition = targetOwner.GlobalPosition;
        return true;
    }

    private void OnTargetDied(Node source)
    {
        ClearTarget();
    }

    private void UnsubscribeFromTarget()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target))
        {
            _target.Died -= OnTargetDied;
        }
    }

    private float ResolveModifiedDamage(HealthComponent target, float damage)
    {
        if (ModifyBasicAttackDamage == null)
        {
            return damage;
        }

        foreach (Delegate modifier in ModifyBasicAttackDamage.GetInvocationList())
        {
            damage = ((Func<HealthComponent, float, float>)modifier).Invoke(target, damage);
        }

        return damage;
    }
}
