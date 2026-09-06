using Godot;

/// <summary>Offline deterministic acceptance harness for reward distribution.</summary>
public partial class ProgressionRewardHarness : Node3D
{
    public override void _Ready()
    {
        CallDeferred(nameof(Run));
    }

    private void Run()
    {
        Node3D player = CreateUnit("Player", 1, 1, 0);
        GoldComponent gold = player.GetNode<GoldComponent>("GoldComponent");
        ProgressionComponent progression = player.GetNode<ProgressionComponent>("ProgressionComponent");
        GD.Print($"[RewardTest] Initial Gold={gold.CurrentGold} XP={progression.CurrentExperience}");
        Kill(CreateUnit("EnemyMinionDummy", 0, 0, 20, 40), player, gold, progression, "Minion");
        Kill(CreateUnit("EnemyChampionDummy", 0, 0, 300, 350), player, gold, progression, "Champion");
        Kill(CreateUnit("EnemyTowerDummy", 0, 0, 250, 500), player, gold, progression, "Tower");
        progression.AddExperience(100000);
        GD.Print($"[RewardTest] LevelCap Level={progression.CurrentLevel} Points={progression.SkillPoints}");
    }

    private Node3D CreateUnit(string name, int withGold, int withProgression, int rewardGold = 0, int rewardXp = 0)
    {
        Node3D unit = new Node3D { Name = name }; AddChild(unit);
        unit.AddChild(new HealthComponent { Name = "HealthComponent", MaxHealth = 100, EnableLegacyPresentation = false });
        if (withGold != 0) unit.AddChild(new GoldComponent { Name = "GoldComponent" });
        if (withProgression != 0) unit.AddChild(new ProgressionComponent { Name = "ProgressionComponent" });
        if (rewardGold != 0 || rewardXp != 0) unit.AddChild(new RewardComponent { Name = "RewardComponent", GoldReward = rewardGold, ExperienceReward = rewardXp });
        return unit;
    }

    private static void Kill(Node3D target, Node3D player, GoldComponent gold, ProgressionComponent xp, string label)
    {
        int beforeGold = gold.CurrentGold, beforeXp = xp.CurrentExperience;
        target.GetNode<HealthComponent>("HealthComponent").TakeDamage(1000, player);
        target.GetNode<HealthComponent>("HealthComponent").TakeDamage(1000, player);
        GD.Print($"[RewardTest] {label} Gold {beforeGold}->{gold.CurrentGold} XP {beforeXp}->{xp.CurrentExperience}");
    }
}
