using Godot;

/// <summary>Target-priority decision layer for a stationary ranged tower.</summary>
public partial class TowerController : StaticBody3D, ITeamMember
{
    [Export] public MinionTeam Team = MinionTeam.Blue;
    [Export] public float DetectionRange = 12.0f;
    [Export] public float AttackRange = 8.0f;
    [Export] public float AttackDamage = 15.0f;
    [Export] public float AttackCooldown = 1.0f;
    public bool RemoteRepresentation { get; set; }
    TeamId ITeamMember.TeamId => Team == MinionTeam.Red ? TeamId.Red : TeamId.Blue;

    private HealthComponent _health;
    private TargetingComponent _targeting;
    private CombatComponent _combat;

    public override void _Ready()
    {
        AddToGroup("combat_units");
        _health = GetNodeOrNull<HealthComponent>("HealthComponent");
        _targeting = GetNodeOrNull<TargetingComponent>("TargetingComponent");
        _combat = GetNodeOrNull<CombatComponent>("CombatComponent");
        EnsureCoreComponents();
        if (GetNodeOrNull<RewardComponent>("RewardComponent") == null) AddChild(new RewardComponent { Name = "RewardComponent", GoldReward = 250, ExperienceReward = 500 });
        _combat.AttackDelivery += DeliverTowerShot;
        ApplyTeamMaterial();
    }

    public override void _ExitTree()
    {
        if (_combat != null) _combat.AttackDelivery -= DeliverTowerShot;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (RemoteRepresentation) return;
        if (_health == null || !_health.IsAlive)
        {
            _targeting?.ClearTarget(); _combat?.ClearTarget(); return;
        }
        HealthComponent target = _targeting.CurrentTarget ?? FindPriorityTarget();
        if (target == null) return;
        _targeting.SetTarget(target); _combat.SetTarget(target);
        if (_combat.IsTargetInRange(GlobalPosition))
        {
            Node3D owner = target.GetParent<Node3D>();
            if (owner != null) LookAt(owner.GlobalPosition, Vector3.Up, true);
            _combat.TryAttack(GlobalPosition);
        }
    }

    private HealthComponent FindPriorityTarget()
    {
        HealthComponent bestMinion = null, bestOther = null;
        float bestMinionDistance = DetectionRange * DetectionRange, bestOtherDistance = bestMinionDistance;
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D candidate || candidate == this || !CombatTeams.IsEnemy(this, candidate)) continue;
            HealthComponent health = candidate.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health == null || !health.IsAlive) continue;
            Vector3 offset = candidate.GlobalPosition - GlobalPosition; offset.Y = 0.0f;
            float distance = offset.LengthSquared(); if (distance > DetectionRange * DetectionRange) continue;
            if (candidate.IsInGroup("minions") && distance < bestMinionDistance) { bestMinion = health; bestMinionDistance = distance; }
            else if (distance < bestOtherDistance) { bestOther = health; bestOtherDistance = distance; }
        }
        return bestMinion ?? bestOther;
    }

    private bool DeliverTowerShot(HealthComponent target, float damage, Node source)
    {
        if (target?.GetParent<Node3D>() is not Node3D targetNode) return false;
        DamageProjectile projectile = new DamageProjectile();
        Color color = Team == MinionTeam.Blue ? new Color(0.25f, 0.7f, 1.0f) : new Color(1.0f, 0.28f, 0.22f);
        projectile.Configure(targetNode, target, source, damage, 18.0f, color, ProjectileVisualType.TowerShot);
        GetParent().AddChild(projectile); projectile.GlobalPosition = GlobalPosition + Vector3.Up * 4.0f;
        return true;
    }

    private void EnsureCoreComponents()
    {
        if (_targeting == null) { _targeting = new TargetingComponent { Name = "TargetingComponent" }; AddChild(_targeting); }
        if (_combat == null) { _combat = new CombatComponent { Name = "CombatComponent" }; AddChild(_combat); }
        _combat.AttackRange = AttackRange; _combat.AttackDamage = AttackDamage; _combat.AttackCooldown = AttackCooldown;
    }

    private void ApplyTeamMaterial()
    {
        MeshInstance3D mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D"); if (mesh == null) return;
        mesh.MaterialOverride = new StandardMaterial3D { AlbedoColor = Team == MinionTeam.Blue ? new Color(0.08f, 0.3f, 0.95f) : new Color(0.95f, 0.08f, 0.12f), Metallic = 0.25f, Roughness = 0.3f };
    }
}
