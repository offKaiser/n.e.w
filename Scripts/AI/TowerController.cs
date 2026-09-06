using Godot;

public partial class TowerController : StaticBody3D
{
    [Export]
    public MinionTeam Team = MinionTeam.Blue;

    [Export]
    public float DetectionRange = 12.0f;

    [Export]
    public float AttackRange = 8.0f;

    [Export]
    public float AttackDamage = 15.0f;

    [Export]
    public float AttackCooldown = 1.0f;

    private HealthComponent _health;
    private Node3D _target;
    private double _nextAttackTime;

    public override void _Ready()
    {
        AddToGroup("combat_units");
        _health = GetNodeOrNull<HealthComponent>("HealthComponent");
        ApplyTeamMaterial();
    }

    public override void _PhysicsProcess(double delta)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive && !network.IsServer)
        {
            return;
        }

        if (_health == null || !_health.IsAlive)
        {
            return;
        }

        _target = FindClosestEnemyUnit();
        if (_target == null)
        {
            return;
        }

        LookAt(_target.GlobalPosition, Vector3.Up, true);
        TryAttackTarget();
    }

    private Node3D FindClosestEnemyUnit()
    {
        Node3D closest = null;
        float closestDistanceSquared = DetectionRange * DetectionRange;

        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D candidate || !IsEnemy(candidate))
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
            HeroController hero => hero.Team != Team,
            EnemyController enemy => enemy.Team != Team,
            _ => false
        };
    }

    private void TryAttackTarget()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 offset = _target.GlobalPosition - GlobalPosition;
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

        _target.GetNode<HealthComponent>("HealthComponent").TakeDamage(AttackDamage, this);
        _nextAttackTime = currentTime + AttackCooldown;
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
            ? new Color(0.08f, 0.3f, 0.95f)
            : new Color(0.95f, 0.08f, 0.12f);
        material.Metallic = 0.25f;
        material.Roughness = 0.3f;
        mesh.MaterialOverride = material;
    }
}
