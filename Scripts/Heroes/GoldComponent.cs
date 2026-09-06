using Godot;

public partial class GoldComponent : Node
{
    public int Gold { get; private set; }

    public void GainGold(int amount)
    {
        if (amount > 0)
        {
            Gold += amount;
            GetNodeOrNull<NetworkManager>("/root/NetworkManager")?.BroadcastGold(GetPath(), Gold);
        }
    }

    public void SynchronizeGold(int gold)
    {
        Gold = Mathf.Max(0, gold);
    }
}
