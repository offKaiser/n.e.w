using Godot;

public enum NyxaraAiState { Idle, AcquireTarget, Chase, Attack, CastAbility, Dead }

/// <summary>Nyxara's decision layer; shared components execute gameplay.</summary>
public partial class EnemyController : CharacterBody3D, IStatusEffectReceiver, ITeamMember
{
    [Export] public MinionTeam Team = MinionTeam.Red;
    [Export] public float DetectionRange = 14.0f;
    public bool RemoteRepresentation { get; set; }
    public NyxaraAiState State { get; private set; } = NyxaraAiState.Idle;
    TeamId ITeamMember.TeamId => Team == MinionTeam.Red ? TeamId.Red : TeamId.Blue;

    private MovementComponent _movement;
    private TargetingComponent _targeting;
    private CombatComponent _combat;
    private HealthComponent _health;
    private AbilityController _abilities;
    private float _slowMultiplier = 1.0f;
    private double _slowEndTime;

    public override void _Ready()
    {
        AddToGroup("combat_units");
        _movement = GetNodeOrNull<MovementComponent>("MovementComponent");
        _targeting = GetNodeOrNull<TargetingComponent>("TargetingComponent");
        _combat = GetNodeOrNull<CombatComponent>("CombatComponent");
        _health = GetNodeOrNull<HealthComponent>("HealthComponent");
        _abilities = GetNodeOrNull<AbilityController>("AbilityController");
        EnsureCoreComponents();
        if (GetNodeOrNull<RewardComponent>("RewardComponent") == null)
            AddChild(new RewardComponent { Name = "RewardComponent", GoldReward = 300, ExperienceReward = 350 });
    }

    public override void _PhysicsProcess(double delta)
    {
        if (RemoteRepresentation) return;
        if (_health == null || !_health.IsAlive)
        {
            State = NyxaraAiState.Dead;
            _targeting?.ClearTarget(); _combat?.ClearTarget(); _movement?.MoveInDirection(Vector3.Zero);
            return;
        }

        HealthComponent target = _targeting.CurrentTarget;
        if (target == null)
        {
            State = NyxaraAiState.AcquireTarget;
            target = FindClosestEnemy();
            if (target == null) { State = NyxaraAiState.Idle; _movement.MoveInDirection(Vector3.Zero); return; }
            _targeting.SetTarget(target);
        }

        _combat.SetTarget(target);
        if (TryUseAbility(target))
        {
            State = NyxaraAiState.CastAbility;
            _movement.MoveInDirection(Vector3.Zero);
            return;
        }

        if (_combat.IsTargetInRange(GlobalPosition))
        {
            State = NyxaraAiState.Attack;
            _movement.MoveInDirection(Vector3.Zero);
            _combat.TryAttack(GlobalPosition);
            return;
        }

        State = NyxaraAiState.Chase;
        _movement.MoveInDirection(_combat.GetApproachDirection(GlobalPosition), GetCurrentSpeed());
    }

    public void ApplySlow(float multiplier, float duration)
    {
        _slowMultiplier = Mathf.Min(_slowMultiplier, multiplier);
        _slowEndTime = Mathf.Max(_slowEndTime, Time.GetTicksMsec() / 1000.0 + duration);
    }

    public void ActivateSpeedBoost(float multiplier, float duration) { }
    public void ApplyAbyssSuppression(float cooldownMultiplier, float duration) { }
    public void ApplyNetworkTransform(Vector3 position, Vector3 rotation) { GlobalPosition = position; Rotation = rotation; }

    private HealthComponent FindClosestEnemy()
    {
        HealthComponent closest = null;
        float bestDistanceSquared = DetectionRange * DetectionRange;
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D candidate || candidate == this || !CombatTeams.IsEnemy(this, candidate)) continue;
            HealthComponent health = candidate.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health == null || !health.IsAlive) continue;
            Vector3 offset = candidate.GlobalPosition - GlobalPosition; offset.Y = 0.0f;
            if (offset.LengthSquared() < bestDistanceSquared) { bestDistanceSquared = offset.LengthSquared(); closest = health; }
        }
        return closest;
    }

    private bool TryUseAbility(HealthComponent target)
    {
        if (_abilities == null || target?.GetParent<Node3D>() is not Node3D targetOwner) return false;
        float distance = GlobalPosition.DistanceTo(targetOwner.GlobalPosition);
        if (distance > _combat.AttackRange + 1.5f && distance < 9.0f && _abilities.TryCast(AbilitySlot.E, target)) return true;
        if (distance > _combat.AttackRange && distance <= 12.0f && _abilities.TryCast(AbilitySlot.R, target)) return true;
        if (distance <= 8.0f && _abilities.TryCast(AbilitySlot.Q, target)) return true;
        return _abilities.TryCast(AbilitySlot.W, target);
    }

    private float GetCurrentSpeed()
    {
        if (Time.GetTicksMsec() / 1000.0 >= _slowEndTime) _slowMultiplier = 1.0f;
        return _movement.Speed * _slowMultiplier;
    }

    private void EnsureCoreComponents()
    {
        if (_movement == null) { _movement = new MovementComponent { Name = "MovementComponent", Speed = 4.0f }; AddChild(_movement); }
        if (_targeting == null) { _targeting = new TargetingComponent { Name = "TargetingComponent" }; AddChild(_targeting); }
        if (_combat == null) { _combat = new CombatComponent { Name = "CombatComponent", AttackDamage = 10.0f, AttackRange = 3.0f, AttackCooldown = 1.0f }; AddChild(_combat); }
        if (GetNodeOrNull<ManaComponent>("ManaComponent") == null) AddChild(new ManaComponent { Name = "ManaComponent", MaxMana = 300.0f });
        EnsureAbilityNodes();
        if (_abilities == null) { _abilities = new AbilityController { Name = "AbilityController" }; AddChild(_abilities); }
        if (GetNodeOrNull<NyxaraPassive>("NyxaraPassive") == null) AddChild(new NyxaraPassive { Name = "NyxaraPassive" });
    }

    private void EnsureAbilityNodes()
    {
        if (GetNodeOrNull<Ability>("AbilityQ") == null) AddChild(new NyxaraGrenadeAbility { Name = "AbilityQ", Cooldown = 8.0f, ManaCost = 35.0f, Range = 8.0f });
        if (GetNodeOrNull<Ability>("AbilityW") == null) AddChild(new NyxaraFuryAbility { Name = "AbilityW", Cooldown = 14.0f, ManaCost = 30.0f });
        if (GetNodeOrNull<Ability>("AbilityE") == null) AddChild(new NyxaraDashAbility { Name = "AbilityE", Cooldown = 11.0f, ManaCost = 40.0f, Range = 9.0f });
        if (GetNodeOrNull<Ability>("AbilityR") == null) AddChild(new NyxaraCannonAbility { Name = "AbilityR", Cooldown = 24.0f, ManaCost = 90.0f, Range = 12.0f });
    }
}
