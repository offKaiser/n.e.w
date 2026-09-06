using Godot;

public enum MinionTeam { Blue, Red }
public enum MinionType { Melee, Tank, Ranged }

/// <summary>Lane/target decision layer. Shared components execute gameplay.</summary>
public partial class MinionController : CharacterBody3D, ITeamMember, IStatusEffectReceiver
{
    [Export] public float Speed = 2.5f;
    [Export] public float DetectionRange = 5.0f;
    [Export] public float AttackRange = 1.6f;
    [Export] public float AttackDamage = 8.0f;
    [Export] public float AttackCooldown = 1.0f;

    [Export] public MinionTeam Team { get; set; }
    [Export] public MinionType Type { get; set; } = MinionType.Melee;
    [Export] public Vector3 LaneDirection { get; set; } = Vector3.Right;
    public bool RemoteRepresentation { get; set; }
    TeamId ITeamMember.TeamId => Team == MinionTeam.Red ? TeamId.Red : TeamId.Blue;

    private HealthComponent _health;
    private MovementComponent _movement;
    private TargetingComponent _targeting;
    private CombatComponent _combat;
    private float _slowMultiplier = 1.0f;
    private double _slowEndTime;

    public void Configure(MinionTeam team, Vector3 laneDirection, MinionType type)
    {
        Team = team; LaneDirection = laneDirection.Normalized(); Type = type;
    }

    public override void _Ready()
    {
        AddToGroup("minions"); AddToGroup("combat_units");
        _health = GetNodeOrNull<HealthComponent>("HealthComponent");
        _movement = GetNodeOrNull<MovementComponent>("MovementComponent");
        _targeting = GetNodeOrNull<TargetingComponent>("TargetingComponent");
        _combat = GetNodeOrNull<CombatComponent>("CombatComponent");
        EnsureCoreComponents();
        ApplyTypeStats(); ApplyTeamMaterial();
        EnsureRewardComponent();
        if (Type == MinionType.Ranged) _combat.AttackDelivery += DeliverRangedAttack;
    }

    public override void _ExitTree()
    {
        if (_combat != null) _combat.AttackDelivery -= DeliverRangedAttack;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (RemoteRepresentation) return;
        if (_health == null || !_health.IsAlive)
        {
            _targeting?.ClearTarget(); _combat?.ClearTarget(); _movement?.MoveInDirection(Vector3.Zero); return;
        }

        HealthComponent target = _targeting.CurrentTarget;
        if (target == null)
        {
            target = FindClosestEnemy();
            if (target == null) { _movement.MoveInDirection(LaneDirection, GetCurrentSpeed()); return; }
            _targeting.SetTarget(target);
        }

        _combat.SetTarget(target);
        if (_combat.IsTargetInRange(GlobalPosition))
        {
            _movement.MoveInDirection(Vector3.Zero);
            _combat.TryAttack(GlobalPosition);
            return;
        }
        _movement.MoveInDirection(_combat.GetApproachDirection(GlobalPosition), GetCurrentSpeed());
    }

    public void ApplySlow(float multiplier, float duration)
    {
        _slowMultiplier = Mathf.Min(_slowMultiplier, multiplier);
        _slowEndTime = Mathf.Max(_slowEndTime, Time.GetTicksMsec() / 1000.0 + duration);
    }
    public void ActivateSpeedBoost(float multiplier, float duration) { }
    public void ApplyNetworkTransform(Vector3 position, Vector3 rotation) { GlobalPosition = position; Rotation = rotation; }

    private HealthComponent FindClosestEnemy()
    {
        HealthComponent closest = null; float best = DetectionRange * DetectionRange;
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D candidate || candidate == this || !CombatTeams.IsEnemy(this, candidate)) continue;
            HealthComponent health = candidate.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health == null || !health.IsAlive) continue;
            Vector3 offset = candidate.GlobalPosition - GlobalPosition; offset.Y = 0;
            if (offset.LengthSquared() < best) { best = offset.LengthSquared(); closest = health; }
        }
        return closest;
    }

    private bool DeliverRangedAttack(HealthComponent target, float damage, Node source)
    {
        if (Type != MinionType.Ranged || target?.GetParent<Node3D>() is not Node3D targetNode) return false;
        DamageProjectile projectile = new DamageProjectile();
        Color color = Team == MinionTeam.Blue ? new Color(0.25f, 0.7f, 1.0f) : new Color(1.0f, 0.28f, 0.22f);
        projectile.Configure(targetNode, target, source, damage, 14.0f, color, ProjectileVisualType.MinionRanged);
        GetParent().AddChild(projectile); projectile.GlobalPosition = GlobalPosition + Vector3.Up * 0.85f;
        return true;
    }

    private void EnsureCoreComponents()
    {
        if (_movement == null) { _movement = new MovementComponent { Name = "MovementComponent" }; AddChild(_movement); }
        if (_targeting == null) { _targeting = new TargetingComponent { Name = "TargetingComponent" }; AddChild(_targeting); }
        if (_combat == null) { _combat = new CombatComponent { Name = "CombatComponent" }; AddChild(_combat); }
    }

    private void ApplyTypeStats()
    {
        switch (Type)
        {
            case MinionType.Tank: Speed = 2.0f; AttackRange = 1.7f; AttackDamage = 10.0f; _health.SetMaxHealth(110.0f); break;
            case MinionType.Ranged: Speed = 2.7f; DetectionRange = 7.0f; AttackRange = 6.0f; AttackDamage = 7.0f; _health.SetMaxHealth(35.0f); break;
            default: _health.SetMaxHealth(50.0f); break;
        }
        _movement.Speed = Speed; _combat.AttackRange = AttackRange; _combat.AttackDamage = AttackDamage; _combat.AttackCooldown = AttackCooldown;
    }

    private void EnsureRewardComponent()
    {
        if (GetNodeOrNull<RewardComponent>("RewardComponent") != null) return;
        RewardComponent reward = new RewardComponent { Name = "RewardComponent" };
        if (Type == MinionType.Ranged) { reward.GoldReward = 14; reward.ExperienceReward = 25; }
        else if (Type == MinionType.Tank) { reward.GoldReward = 25; reward.ExperienceReward = 60; }
        else { reward.GoldReward = 20; reward.ExperienceReward = 40; }
        AddChild(reward);
    }

    private void ApplyTeamMaterial()
    {
        MeshInstance3D mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D"); if (mesh == null) return;
        mesh.MaterialOverride = new StandardMaterial3D { AlbedoColor = Team == MinionTeam.Blue ? new Color(0.12f, 0.42f, 1.0f) : new Color(1.0f, 0.16f, 0.2f), Roughness = 0.45f };
        mesh.Scale = Type == MinionType.Tank ? new Vector3(1.35f, 1.35f, 1.35f) : Type == MinionType.Ranged ? new Vector3(0.8f, 0.8f, 0.8f) : Vector3.One;
    }

    private float GetCurrentSpeed()
    {
        if (Time.GetTicksMsec() / 1000.0 >= _slowEndTime) _slowMultiplier = 1.0f;
        return _movement.Speed * _slowMultiplier;
    }
}
