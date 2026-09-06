using Godot;

/// <summary>Defines what one unit is worth when it dies.</summary>
public partial class RewardComponent : Node
{
    [Export] public int GoldReward;
    [Export] public int ExperienceReward;
    private HealthComponent _health;

    public override void _Ready()
    {
        _health = GetParent()?.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (_health != null) _health.Died += OnOwnerDied;
    }
    public override void _ExitTree() { if (_health != null) _health.Died -= OnOwnerDied; }
    private void OnOwnerDied(Node source)
    {
        MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
        if (peer != null && peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected && !Multiplayer.IsServer()) return;
        RewardDistributor.Distribute(source, GoldReward, ExperienceReward);
    }
}

public static class RewardDistributor
{
    public static void Distribute(Node source, int gold, int experience)
    {
        for (Node current = source; current != null; current = current.GetParent())
        {
            GoldComponent goldComponent = current.GetNodeOrNull<GoldComponent>("GoldComponent");
            ProgressionComponent progression = current.GetNodeOrNull<ProgressionComponent>("ProgressionComponent");
            if (goldComponent != null || progression != null)
            {
                goldComponent?.AddGold(gold);
                progression?.AddExperience(experience);
                return;
            }
        }
    }
}
