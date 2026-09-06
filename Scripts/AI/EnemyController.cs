using Godot;

public partial class EnemyController : CharacterBody3D
{
    [Export]
    public MinionTeam Team = MinionTeam.Red;

    [Export]
    public float DetectionRange = 14.0f;

    [Export]
    public float AttackRange = 3.0f;

    [Export]
    public float Speed = 4.0f;

    [Export]
    public float AttackDamage = 10.0f;

    [Export]
    public float AttackCooldown = 1.0f;

    private Node3D _target;
    private HealthComponent _targetHealth;
    private double _nextAttackTime;
    private float _slowMultiplier = 1.0f;
    private double _slowEndTime;
    private int _basicAttackCount;
    private double _nextGrenadeTime;
    private double _nextFuryTime;
    private double _nextDashTime;
    private double _nextCannonTime;
    private double _furyEndTime;
    private double _nextNetworkSyncTime;
    private float _abilityCooldownMultiplier = 1.0f;
    private double _suppressionEndTime;

    public override void _Ready()
    {
        AddToGroup("combat_units");
    }

    public override void _PhysicsProcess(double delta)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive && !network.IsServer)
        {
            return;
        }

        FindClosestTarget();

        if (_target == null || _targetHealth == null)
        {
            Velocity = Vector3.Zero;
            PublishNetworkTransform(network);
            return;
        }

        Vector3 offset = _target.GlobalPosition - GlobalPosition;
        offset.Y = 0.0f;

        if (offset.LengthSquared() > DetectionRange * DetectionRange)
        {
            Velocity = Vector3.Zero;
            PublishNetworkTransform(network);
            return;
        }

        if (offset.LengthSquared() > AttackRange * AttackRange)
        {
            TryUseNyxaraSkills(offset);
            Vector3 direction = offset.Normalized();
            LookAt(GlobalPosition + direction, Vector3.Up, true);
            Velocity = direction * GetCurrentSpeed();
            MoveAndSlide();
            PublishNetworkTransform(network);
            return;
        }

        LookAt(_target.GlobalPosition, Vector3.Up, true);
        Velocity = Vector3.Zero;
        TryUseNyxaraSkills(offset);
        TryAttack();
        PublishNetworkTransform(network);
    }

    private void FindClosestTarget()
    {
        Node3D closestTarget = null;
        HealthComponent closestHealth = null;
        float closestDistanceSquared = DetectionRange * DetectionRange;

        foreach (Node node in GetTree().GetNodesInGroup("heroes"))
        {
            if (node is not Node3D candidate)
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
                closestTarget = candidate;
                closestHealth = health;
                closestDistanceSquared = distanceSquared;
            }
        }

        _target = closestTarget;
        _targetHealth = closestHealth;
    }

    private void TryAttack()
    {
        double currentTime = Time.GetTicksMsec() / 1000.0;
        if (currentTime < _nextAttackTime)
        {
            return;
        }

        _basicAttackCount++;
        float damage = AttackDamage;
        if (_basicAttackCount >= 3)
        {
            // Precisão Sombria: every third basic attack deals an extra true-like hit.
            damage += AttackDamage * 0.8f;
            _basicAttackCount = 0;
            SpawnVfx(_target.GlobalPosition + Vector3.Up, new Color(0.95f, 0.85f, 1.0f), 1.15f, 0.38f);
        }
        _targetHealth.TakeDamage(damage, this);
        SpawnVfx(_target.GlobalPosition + Vector3.Up, new Color(0.55f, 0.12f, 0.95f), 0.8f, 0.25f);
        _nextAttackTime = currentTime + GetCurrentAttackCooldown();
    }

    public void ApplySlow(float multiplier, float duration)
    {
        _slowMultiplier = Mathf.Min(_slowMultiplier, multiplier);
        _slowEndTime = Time.GetTicksMsec() / 1000.0 + duration;
    }

    public void ApplyAbyssSuppression(float cooldownMultiplier, float duration)
    {
        _abilityCooldownMultiplier = Mathf.Max(_abilityCooldownMultiplier, cooldownMultiplier);
        _suppressionEndTime = Time.GetTicksMsec() / 1000.0 + duration;
    }

    public void ApplyNetworkTransform(Vector3 position, Vector3 rotation)
    {
        GlobalPosition = position;
        Rotation = rotation;
    }

    private void PublishNetworkTransform(NetworkManager network)
    {
        if (network == null || !network.SessionActive || !network.IsServer || Time.GetTicksMsec() / 1000.0 < _nextNetworkSyncTime) return;
        network.PublishEnemyTransform(Name, GlobalPosition, Rotation);
        _nextNetworkSyncTime = Time.GetTicksMsec() / 1000.0 + 0.05;
    }

    private float GetCurrentSpeed()
    {
        if (Time.GetTicksMsec() / 1000.0 >= _slowEndTime) _slowMultiplier = 1.0f;
        return Speed * _slowMultiplier;
    }

    private float GetCurrentAttackCooldown()
    {
        return Time.GetTicksMsec() / 1000.0 < _furyEndTime ? AttackCooldown / 1.6f : AttackCooldown;
    }

    private float GetAbilityCooldown(float baseCooldown)
    {
        if (Time.GetTicksMsec() / 1000.0 >= _suppressionEndTime) _abilityCooldownMultiplier = 1.0f;
        return baseCooldown * _abilityCooldownMultiplier;
    }

    // Nyxara's kit is AI-driven for now; its timings and outcomes match her champion card.
    private void TryUseNyxaraSkills(Vector3 offset)
    {
        if (_target == null || _targetHealth == null || !_targetHealth.IsAlive) return;
        double now = Time.GetTicksMsec() / 1000.0;
        float distance = offset.Length();

        // Q - Grenada Sombria: area hit and 40% slow.
        if (distance <= 8.0f && now >= _nextGrenadeTime)
        {
            CastShadowGrenade(_target.GlobalPosition);
            SpawnVfx(_target.GlobalPosition + Vector3.Up * 0.35f, new Color(0.42f, 0.04f, 0.95f), 2.3f, 0.7f);
            _nextGrenadeTime = now + GetAbilityCooldown(8.0f);
        }

        // W - Fúria Celeste: temporary attack-speed amplification.
        if (now >= _nextFuryTime)
        {
            _furyEndTime = now + 4.0;
            SpawnVfx(GlobalPosition + Vector3.Up, new Color(0.65f, 0.12f, 1.0f), 1.7f, 0.55f);
            _nextFuryTime = now + GetAbilityCooldown(14.0f);
        }

        // E - Passo Sombrio: a short reposition toward its current prey.
        if (distance > AttackRange + 1.5f && distance < 9.0f && now >= _nextDashTime)
        {
            Vector3 direction = offset.Normalized();
            GlobalPosition += direction * Mathf.Min(3.5f, distance - AttackRange);
            SpawnVfx(GlobalPosition + Vector3.Up * 0.45f, new Color(0.55f, 0.06f, 1.0f), 1.5f, 0.42f);
            _nextDashTime = now + GetAbilityCooldown(11.0f);
        }

        // R - Canhão Estelar: piercing long-range shot. There is one hero target in this prototype.
        if (distance > AttackRange && distance <= 12.0f && now >= _nextCannonTime)
        {
            Vector3 start = GlobalPosition;
            Vector3 end = _target.GlobalPosition;
            foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
            {
                if (node is not Node3D unit || !CombatTeams.IsEnemy(this, unit)) continue;
                HealthComponent health = unit.GetNodeOrNull<HealthComponent>("HealthComponent");
                if (health != null && health.IsAlive && DistanceToSegment(unit.GlobalPosition, start, end) <= 0.8f)
                    health.TakeDamage(150.0f, this);
            }
            BeamVfx cannon = new BeamVfx();
            GetParent().AddChild(cannon);
            cannon.Configure(GlobalPosition + Vector3.Up, _target.GlobalPosition + Vector3.Up, new Color(0.72f, 0.18f, 1.0f), 0.22f, 0.48f);
            GetNodeOrNull<NetworkManager>("/root/NetworkManager")?.BroadcastBeamVfx(GlobalPosition + Vector3.Up, _target.GlobalPosition + Vector3.Up, new Color(0.72f, 0.18f, 1.0f), 0.22f, 0.48f);
            _nextCannonTime = now + GetAbilityCooldown(24.0f);
        }
    }

    private void SpawnVfx(Vector3 position, Color color, float radius, float duration)
    {
        TimedVfx vfx = new TimedVfx();
        GetParent().AddChild(vfx);
        vfx.GlobalPosition = position;
        vfx.Configure(color, radius, duration);
        GetNodeOrNull<NetworkManager>("/root/NetworkManager")?.BroadcastTimedVfx(position, color, radius, duration);
    }

    private void CastShadowGrenade(Vector3 center)
    {
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D unit || !CombatTeams.IsEnemy(this, unit)) continue;
            Vector3 offset = unit.GlobalPosition - center; offset.Y = 0;
            if (offset.LengthSquared() > 2.3f * 2.3f) continue;
            HealthComponent health = unit.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health == null || !health.IsAlive) continue;
            health.TakeDamage(18.0f, this);
            if (unit is HeroController hero) hero.ApplySlow(0.6f, 2.0f);
            if (unit is MinionController minion) minion.ApplySlow(0.6f, 2.0f);
        }
    }

    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.LengthSquared();
        if (lengthSquared <= 0.001f) return point.DistanceTo(start);
        float t = Mathf.Clamp((point - start).Dot(segment) / lengthSquared, 0.0f, 1.0f);
        return point.DistanceTo(start + segment * t);
    }
}
