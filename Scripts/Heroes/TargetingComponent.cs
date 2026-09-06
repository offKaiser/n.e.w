using Godot;

/// <summary>
/// Stores and validates the current target. It deliberately has no attack or
/// damage logic so it can be shared by player controllers and AI later.
/// </summary>
public partial class TargetingComponent : Node
{
    private HealthComponent _currentTarget;

    public HealthComponent CurrentTarget
    {
        get
        {
            if (!HasValidTarget)
            {
                ClearTarget();
            }

            return _currentTarget;
        }
    }

    public bool HasValidTarget => _currentTarget != null &&
                                  GodotObject.IsInstanceValid(_currentTarget) &&
                                  _currentTarget.IsAlive;

    public void SetTarget(HealthComponent target)
    {
        _currentTarget = target != null && target.IsAlive ? target : null;
    }

    public void ClearTarget()
    {
        _currentTarget = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!HasValidTarget)
        {
            ClearTarget();
        }
    }
}
