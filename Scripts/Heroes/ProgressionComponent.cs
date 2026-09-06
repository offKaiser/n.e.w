using System;
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
    public int CurrentLevel => Level;
    public int CurrentExperience => Experience;
    public int SkillPoints { get; private set; } = 1;
    public int ExperienceToNextLevel => GetExperienceToNextLevel();
    public event Action<int, int> ExperienceChanged;
    public event Action<int> LevelChanged;
    public event Action<int> SkillPointsChanged;

    public void GainExperience(int amount) => AddExperience(amount);

    public void AddExperience(int amount)
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
            LevelChanged?.Invoke(Level);
            SkillPointsChanged?.Invoke(SkillPoints);
            GD.Print($"{GetParent().Name} alcancou o nivel {Level}.");
        }
        ExperienceChanged?.Invoke(Experience, ExperienceToNextLevel);
    }

    public bool TrySpendSkillPoint()
    {
        if (SkillPoints <= 0)
        {
            return false;
        }

        SkillPoints--;
        SkillPointsChanged?.Invoke(SkillPoints);
        return true;
    }

    public bool CanSpendSkillPoint() => SkillPoints > 0;

    public void SynchronizeProgression(int level, int experience, int skillPoints)
    {
        Level = level;
        Experience = experience;
        SkillPoints = skillPoints;
        ExperienceChanged?.Invoke(Experience, ExperienceToNextLevel);
        LevelChanged?.Invoke(Level);
        SkillPointsChanged?.Invoke(SkillPoints);
    }

    private int GetExperienceToNextLevel()
    {
        return ExperienceForFirstLevel + (Level - 1) * ExperienceGrowthPerLevel;
    }
}
