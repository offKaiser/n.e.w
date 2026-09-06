using Godot;

public partial class ManaComponent : Node
{
    [Export]
    public float MaxMana = 100.0f;

    public float CurrentMana { get; private set; }

    public override void _Ready()
    {
        CurrentMana = MaxMana;
    }

    public bool TrySpend(float amount)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive && !network.IsServer)
        {
            network.RequestManaSpend(GetParent().GetPath(), amount);
            return amount <= CurrentMana;
        }

        bool spent = SpendMana(amount);
        if (spent)
        {
            network?.BroadcastMana(GetParent().GetPath(), CurrentMana);
        }

        return spent;
    }

    public bool SpendMana(float amount)
    {
        if (amount > CurrentMana)
        {
            return false;
        }

        CurrentMana -= amount;
        return true;
    }

    public void SynchronizeMana(float mana)
    {
        CurrentMana = Mathf.Clamp(mana, 0.0f, MaxMana);
    }
}
