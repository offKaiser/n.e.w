using System;
using Godot;

public partial class GoldComponent : Node
{
    public int Gold { get; private set; }
    public int CurrentGold => Gold;
    public event Action<int> GoldChanged;

    public void GainGold(int amount)
    {
        if (amount > 0)
        {
            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }
    }

    public void AddGold(int amount) => GainGold(amount);
    public bool TrySpendGold(int amount) { if (amount <= 0 || amount > Gold) return false; Gold -= amount; GoldChanged?.Invoke(Gold); return true; }
    public bool SpendGold(int amount) => TrySpendGold(amount); // legacy adapter

    public void SynchronizeGold(int gold)
    {
        Gold = Mathf.Max(0, gold);
        GoldChanged?.Invoke(Gold);
    }
}
