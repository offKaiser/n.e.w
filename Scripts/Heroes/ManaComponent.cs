using System;
using Godot;

public partial class ManaComponent : Node
{
    [Export]
    public float MaxMana = 100.0f;

    public float CurrentMana { get; private set; }
    public event Action<float, float> ManaChanged;

    public override void _Ready()
    {
        CurrentMana = MaxMana;
        ManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    public bool CanSpend(float amount) => amount >= 0.0f && CurrentMana >= amount;

    public bool TrySpend(float amount)
    {
        MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
        if (peer != null && peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected && !Multiplayer.IsServer())
        {
            // Spending is a host-side consequence of AbilityCast intent.
            return false;
        }

        return SpendMana(amount);
    }

    public bool SpendMana(float amount)
    {
        if (!CanSpend(amount))
        {
            return false;
        }

        CurrentMana -= amount;
        ManaChanged?.Invoke(CurrentMana, MaxMana);
        return true;
    }

    public void Restore(float amount)
    {
        if (amount <= 0.0f)
        {
            return;
        }

        CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
        ManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    public void SetMaxMana(float maximum, bool refill = true)
    {
        MaxMana = Mathf.Max(maximum, 1.0f);
        CurrentMana = refill ? MaxMana : Mathf.Min(CurrentMana, MaxMana);
        ManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    public void SynchronizeMana(float mana)
    {
        CurrentMana = Mathf.Clamp(mana, 0.0f, MaxMana);
        ManaChanged?.Invoke(CurrentMana, MaxMana);
    }
}
