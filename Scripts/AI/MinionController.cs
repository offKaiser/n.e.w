using Godot;

public enum MinionTeam
{
    Blue,
    Red
}

public enum MinionType
{
    Melee,
    Tank,
    Ranged
}

public partial class MinionController : CharacterBody3D
{
    [Export]
    public float Speed = 2.5f;

    [Export]
    public float DetectionRange = 5.0f;

    [Export]
    public float AttackRange = 1.6f;

    [Export]
    public float AttackDamage = 8.0f;

    [Export]
    public float AttackCooldown = 1.0f;

    public MinionTeam Team { get; private set; }
    public MinionType Type { get; private set; } = MinionType.Melee;
    public Vector3 LaneDirection { get; private set; } = Vector3.Right;

    private HealthComponent _health;
    private Node3D _target;
    private double _nextAttackTime;
    private float _slowMultiplier = 1.0f;
    private double _slowEndTime;
    private double _nextNetworkSyncTime;

    public void Configure(MinionTeam team, Vector3 laneDirection, MinionType type)
    {
        Team = team;
        LaneDirection = laneDirection.Normalized();
        Type = type;
    }

    public override void _Ready()
    {
        AddToGroup("minions");
        AddToGroup("combat_units");
        _health = GetNodeOrNull<HealthComponent>("HealthComponent");
        ApplyTypeStats();
        ApplyTeamMaterial();
    }

    public override void _PhysicsProcess(double delta)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive && !network.IsServer) return;
        if (_health == null || !_health.IsAlive)
        {
            Velocity = Vector3.Zero;
            PublishNetworkTransform(network); return;
        }

        _target = FindClosestEnemy();
        if (_target == null)
        {
            MoveAlongLane();
            PublishNetworkTransform(network); return;
        }

        Vector3 offset = _target.GlobalPosition - GlobalPosition;
        offset.Y = 0.0f;

        if (offset.LengthSquared() > AttackRange * AttackRange)
        {
            Vector3 direction = offset.Normalized();
            LookAt(GlobalPosition + direction, Vector3.Up, true);
            Velocity = direction * GetCurrentSpeed();
            MoveAndSlide();
            PublishNetworkTransform(network); return;
        }

        LookAt(_target.GlobalPosition, Vector3.Up, true);
        Velocity = Vector3.Zero;
        TryAttackTarget();
        PublishNetworkTransform(network);
    }

    public void ApplyNetworkTransform(Vector3 position, Vector3 rotation)
    {
        GlobalPosition = position;
        Rotation = rotation;
    }

    private void PublishNetworkTransform(NetworkManager network)
    {
        if (network == null || !network.SessionActive || !network.IsServer || Time.GetTicksMsec() / 1000.0 < _nextNetworkSyncTime) return;
        network.PublishMinionTransform(Name, GlobalPosition, Rotation);
        _nextNetworkSyncTime = Time.GetTicksMsec() / 1000.0 + 0.1;
    }

    private Node3D FindClosestEnemy()
    {
        Node3D closest = null;
        float closestDistanceSquared = DetectionRange * DetectionRange;

        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D candidate || candidate == this || !IsEnemy(candidate))
            {
                continue;
            }

            HealthComponent health = candidate.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health == null || !health.IsAlive)
            {
                continue;
            }

            Vector3 offset = candidate.GlobalPosition - GlobalPosition;
            offset.Y = 0.0f;
            float distanceSquared = offset.LengthSquared();
            if (distanceSquared < closestDistanceSquared)
            {
                closest = candidate;
                closestDistanceSquared = distanceSquared;
            }
        }

        return closest;
    }

    private bool IsEnemy(Node3D candidate)
    {
        return candidate switch
        {
            MinionController minion => minion.Team != Team,
            TowerController tower => tower.Team != Team,
            _ => false
        };
    }

    private void MoveAlongLane()
    {
        LookAt(GlobalPosition + LaneDirection, Vector3.Up, true);
        Velocity = LaneDirection * GetCurrentSpeed();
        MoveAndSlide();
    }

    private void TryAttackTarget()
    {
        double currentTime = Time.GetTicksMsec() / 1000.0;
        if (currentTime < _nextAttackTime)
        {
            return;
        }

        HealthComponent targetHealth = _target.GetNode<HealthComponent>("HealthComponent");
        if (Type == MinionType.Ranged)
        {
            LaunchProjectile(targetHealth);
        }
        else
        {
            targetHealth.TakeDamage(AttackDamage, this);
        }
        _nextAttackTime = currentTime + AttackCooldown;
    }

    private void LaunchProjectile(HealthComponent targetHealth)
    {
        DamageProjectile projectile = new DamageProjectile();
        Color projectileColor = Team == MinionTeam.Blue ? new Color(0.25f, 0.7f, 1.0f) : new Color(1.0f, 0.28f, 0.22f);
        projectile.Configure(_target, targetHealth, this, AttackDamage, 14.0f, projectileColor);
        GetParent().AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition + Vector3.Up * 0.85f;
    }

    private void ApplyTeamMaterial()
    {
        MeshInstance3D mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        if (mesh == null)
        {
            return;
        }

        StandardMaterial3D material = new StandardMaterial3D();
        material.AlbedoColor = Team == MinionTeam.Blue
            ? new Color(0.12f, 0.42f, 1.0f)
            : new Color(1.0f, 0.16f, 0.2f);
        material.Roughness = 0.45f;
        mesh.MaterialOverride = material;
        mesh.Scale = Type switch
        {
            MinionType.Tank => new Vector3(1.35f, 1.35f, 1.35f),
            MinionType.Ranged => new Vector3(0.8f, 0.8f, 0.8f),
            _ => Vector3.One
        };
    }

    private void ApplyTypeStats()
    {
        switch (Type)
        {
            case MinionType.Tank:
                Speed = 2.0f;
                AttackRange = 1.7f;
                AttackDamage = 10.0f;
                _health.SetMaxHealth(110.0f);
                break;
            case MinionType.Ranged:
                Speed = 2.7f;
                DetectionRange = 7.0f;
                AttackRange = 6.0f;
                AttackDamage = 7.0f;
                _health.SetMaxHealth(35.0f);
                break;
            default:
                _health.SetMaxHealth(50.0f);
                break;
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        _slowMultiplier = Mathf.Min(_slowMultiplier, multiplier);
        _slowEndTime = Time.GetTicksMsec() / 1000.0 + duration;
    }

    private float GetCurrentSpeed()
    {
        if (Time.GetTicksMsec() / 1000.0 >= _slowEndTime) _slowMultiplier = 1.0f;
        return Speed * _slowMultiplier;
    }
}
