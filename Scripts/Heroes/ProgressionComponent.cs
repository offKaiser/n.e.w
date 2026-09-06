using Godot;

public partial class ProgressionComponent : Node
{
    [Export]
    public int MaxLevel = 16;

    [Export]
    public int ExperienceForFirstLevel = 100;

    [Export]
    public int ExperienceGrowthPerLevel = 50;

    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int SkillPoints { get; private set; } = 1;
    public int ExperienceToNextLevel => GetExperienceToNextLevel();

    public void GainExperience(int amount)
    {
        if (amount <= 0 || Level >= MaxLevel)
        {
            return;
        }

        Experience += amount;
        while (Level < MaxLevel && Experience >= GetExperienceToNextLevel())
        {
            Experience -= GetExperienceToNextLevel();
            Level++;
            SkillPoints++;
            GD.Print($"{GetParent().Name} alcancou o nivel {Level}.");
        }
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        network?.BroadcastProgression(GetPath(), Level, Experience, SkillPoints);
    }

    public bool TrySpendSkillPoint()
    {
        if (SkillPoints <= 0)
        {
            return false;
        }

        SkillPoints--;
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        network?.BroadcastProgression(GetPath(), Level, Experience, SkillPoints);
        return true;
    }

    public void SynchronizeProgression(int level, int experience, int skillPoints)
    {
        Level = level;
        Experience = experience;
        SkillPoints = skillPoints;
    }

    private int GetExperienceToNextLevel()
    {
        return ExperienceForFirstLevel + (Level - 1) * ExperienceGrowthPerLevel;
    }
}
