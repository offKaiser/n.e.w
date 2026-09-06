using System;
using Godot;

public partial class HealthComponent : Node
{
    [Export]
    public float MaxHealth = 100.0f;

    [Export]
    public bool EnableLegacyPresentation = true;

    [Export]
    public NodePath HealthLabelPath;


    public float CurrentHealth { get; private set; }
    public bool IsAlive => !_isDead;

    public event Action<float, float> HealthChanged;
    public event Action<float, Node> Damaged;
    public event Action<Node> Died;

    private Label3D _healthLabel;
    private Node _lastDamageSource;
    private float _damageTakenMultiplier = 1.0f;
    private double _damageReductionEndTime;
    private bool _isDead;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
        _isDead = false;
        if (EnableLegacyPresentation)
        {
            _healthLabel = GetNodeOrNull<Label3D>(HealthLabelPath);
        }
        NotifyHealthChanged();
    }

    public void TakeDamage(float damage, Node source = null)
    {
        if (IsRemoteMultiplayerClient())
        {
            // Remote clients never authoritatively apply combat results. The
            // host's state/event replication supplies this presentation state.
            return;
        }

        ApplyDamage(damage, source);
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0.0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        NotifyHealthChanged();
    }

    public void SynchronizeHealth(float health)
    {
        if (_isDead)
        {
            return;
        }

        CurrentHealth = Mathf.Clamp(health, 0.0f, MaxHealth);
        NotifyHealthChanged();
        if (CurrentHealth <= 0.0f)
        {
            Die(null, false);
        }
    }

    private void ApplyDamage(float damage, Node source)
    {
        if (!IsAlive || damage <= 0.0f)
        {
            return;
        }

        _lastDamageSource = source ?? _lastDamageSource;
        if (Time.GetTicksMsec() / 1000.0 >= _damageReductionEndTime) _damageTakenMultiplier = 1.0f;
        float appliedDamage = damage * _damageTakenMultiplier;
        CurrentHealth = Mathf.Max(CurrentHealth - appliedDamage, 0.0f);
        Damaged?.Invoke(appliedDamage, source);
        NotifyHealthChanged();

        if (CurrentHealth <= 0.0f)
        {
            Die(_lastDamageSource, true);
        }
    }

    public void SetMaxHealth(float newMaxHealth, bool refill = true)
    {
        MaxHealth = Mathf.Max(newMaxHealth, 1.0f);
        CurrentHealth = refill ? MaxHealth : Mathf.Min(CurrentHealth, MaxHealth);
        NotifyHealthChanged();
    }

    public void ApplyDamageReduction(float damageMultiplier, float duration)
    {
        _damageTakenMultiplier = Mathf.Min(_damageTakenMultiplier, damageMultiplier);
        _damageReductionEndTime = Mathf.Max(_damageReductionEndTime, Time.GetTicksMsec() / 1000.0 + duration);
    }

    private void Die(Node source, bool grantLegacyRewards)
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        Died?.Invoke(source);

        if (EnableLegacyPresentation)
        {
            DefeatOwner();
        }
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
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

    private bool IsRemoteMultiplayerClient()
    {
        MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
        return peer != null && peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected && !Multiplayer.IsServer();
    }
}
