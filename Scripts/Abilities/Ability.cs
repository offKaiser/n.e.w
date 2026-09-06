using Godot;

public enum AbilityTargetType
{
    Enemy,
    Ground,
    Self
}

public abstract partial class Ability : Node
{
    [Export]
    public float Cooldown = 3.0f;

    [Export]
    public float ManaCost = 20.0f;

    [Export]
    public float Range = 8.0f;

    [Export]
    public AbilityTargetType TargetType = AbilityTargetType.Enemy;

    [Export] public int MaxRank = 5;
    [Export] public float RankDamageBonus = 0.15f;
    public int Rank { get; private set; } = 1;
    public float RankMultiplier => 1.0f + (Rank - 1) * RankDamageBonus;

    private double _nextCastTime;

    public float RemainingCooldown => Mathf.Max(0.0f, (float)(_nextCastTime - Time.GetTicksMsec() / 1000.0));

    public void SynchronizeCooldown(float remaining)
    {
        _nextCastTime = Time.GetTicksMsec() / 1000.0 + Mathf.Max(0.0f, remaining);
    }

    public void ReduceCooldown(float seconds)
    {
        _nextCastTime = Mathf.Max(Time.GetTicksMsec() / 1000.0, _nextCastTime - seconds);
    }

    public bool TryCast(Node3D caster, HealthComponent target)
    {
        if (!CanCast(caster, target))
        {
            return false;
        }

        ManaComponent mana = caster.GetNodeOrNull<ManaComponent>("ManaComponent");
        if (mana != null && !mana.TrySpend(ManaCost))
        {
            return false;
        }

        if (!Execute(caster, target))
        {
            return false;
        }

        _nextCastTime = Time.GetTicksMsec() / 1000.0 + Cooldown;
        return true;
    }

    public bool TryIncreaseRank(ProgressionComponent progression)
    {
        if (Rank >= MaxRank || !progression.TrySpendSkillPoint()) return false;
        Rank++;
        return true;
    }

    public void SynchronizeRank(int rank)
    {
        Rank = Mathf.Clamp(rank, 1, MaxRank);
    }

    protected abstract bool Execute(Node3D caster, HealthComponent target);

    private bool CanCast(Node3D caster, HealthComponent target)
    {
        if (Time.GetTicksMsec() / 1000.0 < _nextCastTime)
        {
            return false;
        }

        if (TargetType == AbilityTargetType.Self)
        {
            return true;
        }

        if (TargetType == AbilityTargetType.Enemy && (target == null || !target.IsAlive))
        {
            return false;
        }

        if (target == null)
        {
            return false;
        }

        Vector3 offset = target.GetParent<Node3D>().GlobalPosition - caster.GlobalPosition;
        offset.Y = 0.0f;
        return offset.LengthSquared() <= Range * Range;
    }
}
