using Godot;

public partial class HealthComponent : Node
{
    [Export]
    public float MaxHealth = 100.0f;

    [Export]
    public NodePath HealthLabelPath;

    [Export]
    public int ExperienceReward;

    [Export]
    public int GoldReward;

    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0.0f;

    private Label3D _healthLabel;
    private Node _lastDamageSource;
    private float _damageTakenMultiplier = 1.0f;
    private double _damageReductionEndTime;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        _healthLabel = GetNodeOrNull<Label3D>(HealthLabelPath);
        UpdateHealthLabel();
    }

    public void TakeDamage(float damage, Node source = null)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive && !network.IsServer)
        {
            network.RequestDamage(GetParent().GetPath(), damage, source?.GetPath() ?? new NodePath());
            return;
        }

        ApplyDamage(damage, source, network);
    }

    public void SynchronizeHealth(float health)
    {
        CurrentHealth = Mathf.Max(health, 0.0f);
        UpdateHealthLabel();
        if (!IsAlive)
        {
            DefeatOwner();
        }
    }

    private void ApplyDamage(float damage, Node source, NetworkManager network)
    {
        if (!IsAlive || damage <= 0.0f)
        {
            return;
        }

        _lastDamageSource = source ?? _lastDamageSource;
        if (Time.GetTicksMsec() / 1000.0 >= _damageReductionEndTime) _damageTakenMultiplier = 1.0f;
        CurrentHealth = Mathf.Max(CurrentHealth - damage * _damageTakenMultiplier, 0.0f);
        UpdateHealthLabel();

        if (!IsAlive)
        {
            RewardKiller();
            DefeatOwner();
        }

        network?.BroadcastHealth(GetParent().GetPath(), CurrentHealth);
    }

    public void SetMaxHealth(float newMaxHealth, bool refill = true)
    {
        MaxHealth = newMaxHealth;
        CurrentHealth = refill ? MaxHealth : Mathf.Min(CurrentHealth, MaxHealth);
        UpdateHealthLabel();
    }

    public void ApplyDamageReduction(float damageMultiplier, float duration)
    {
        _damageTakenMultiplier = Mathf.Min(_damageTakenMultiplier, damageMultiplier);
        _damageReductionEndTime = Mathf.Max(_damageReductionEndTime, Time.GetTicksMsec() / 1000.0 + duration);
    }

    private void RewardKiller()
    {
        GetParent().GetNodeOrNull<AbyssMark>("AbyssMark")?.RewardIfKilledBy(_lastDamageSource);
        foreach (Node node in GetTree().GetNodesInGroup("abyss_fields"))
        {
            if (node is AbyssField field) field.ExtendIfOwnedBy(_lastDamageSource);
        }
        Node recipient = _lastDamageSource;
        while (recipient != null)
        {
            ProgressionComponent progression = recipient.GetNodeOrNull<ProgressionComponent>("ProgressionComponent");
            GoldComponent gold = recipient.GetNodeOrNull<GoldComponent>("GoldComponent");
            if (progression != null || gold != null)
            {
                progression?.GainExperience(ExperienceReward);
                gold?.GainGold(GoldReward);
                return;
            }

            recipient = recipient.GetParent();
        }
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel != null)
        {
            _healthLabel.Text = $"{Mathf.CeilToInt(CurrentHealth)} / {Mathf.CeilToInt(MaxHealth)} HP";
        }
    }

    private void DefeatOwner()
    {
        if (GetParent() is not Node3D owner)
        {
            return;
        }

        owner.Visible = false;

        if (owner is CollisionObject3D collisionObject)
        {
            collisionObject.CollisionLayer = 0;
            collisionObject.CollisionMask = 0;
        }
    }
}
