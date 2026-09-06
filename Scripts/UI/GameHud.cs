using Godot;

public partial class GameHud : Control
{
    [Export] public NodePath HeroPath;

    private HealthComponent _health;
    private ManaComponent _mana;
    private Ability _abilityQ;
    private Ability _abilityW;
    private Ability _abilityE;
    private Ability _abilityR;
    private ProgressBar _healthBar;
    private ProgressBar _manaBar;
    private Label _healthText;
    private Label _manaText;
    private Label _abilitiesText;
    private Label _progressionText;
    private ProgressionComponent _progression;
    private GoldComponent _gold;
    private AbyssEnergyComponent _abyssEnergy;

    public override void _Ready()
    {
        Node hero = GetNode<Node>(HeroPath);
        _health = hero.GetNode<HealthComponent>("HealthComponent");
        _mana = hero.GetNode<ManaComponent>("ManaComponent");
        _abilityQ = hero.GetNode<Ability>("AbilityQ");
        _abilityW = hero.GetNode<Ability>("AbilityW");
        _abilityE = hero.GetNode<Ability>("AbilityE");
        _abilityR = hero.GetNode<Ability>("AbilityR");
        _progression = hero.GetNode<ProgressionComponent>("ProgressionComponent");
        _gold = hero.GetNode<GoldComponent>("GoldComponent");
        _abyssEnergy = hero.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent");
        CreateHud();
    }

    public override void _Process(double delta)
    {
        _healthBar.Value = _health.CurrentHealth;
        _manaBar.Value = _mana.CurrentMana;
        _healthText.Text = $"VIDA  {Mathf.CeilToInt(_health.CurrentHealth)} / {Mathf.CeilToInt(_health.MaxHealth)}";
        _manaText.Text = $"MANA  {_mana.CurrentMana:0} / {_mana.MaxMana:0}";
        _abilitiesText.Text = string.Join("    ", new[] { GetAbilityText("Q", _abilityQ), GetAbilityText("W", _abilityW), GetAbilityText("E", _abilityE), GetAbilityText("R", _abilityR) });
        string passive = _abyssEnergy == null ? string.Empty : $"  |  ABISMO {_abyssEnergy.Energy:0}/{_abyssEnergy.MaximumEnergy:0}";
        _progressionText.Text = $"NIVEL {_progression.Level}  |  XP {_progression.Experience} / {_progression.ExperienceToNextLevel}  |  OURO {_gold.Gold}  |  PONTOS {_progression.SkillPoints}{passive}";
    }

    private void CreateHud()
    {
        ColorRect panel = new ColorRect { Color = new Color(0.025f, 0.04f, 0.07f, 0.88f) };
        panel.SetAnchorsPreset(LayoutPreset.BottomLeft);
        panel.Position = new Vector2(22.0f, -177.0f);
        panel.Size = new Vector2(620.0f, 155.0f);
        AddChild(panel);

        _healthText = CreateLabel(new Vector2(16, 12), new Color(1, 0.8f, 0.8f));
        panel.AddChild(_healthText);
        _healthBar = CreateBar(new Vector2(16, 34), new Color(0.9f, 0.1f, 0.15f), _health.MaxHealth);
        panel.AddChild(_healthBar);
        _manaText = CreateLabel(new Vector2(16, 61), new Color(0.7f, 0.85f, 1));
        panel.AddChild(_manaText);
        _manaBar = CreateBar(new Vector2(16, 83), new Color(0.1f, 0.45f, 1), _mana.MaxMana);
        panel.AddChild(_manaBar);
        _abilitiesText = CreateLabel(new Vector2(16, 106), new Color(1, 0.92f, 0.45f));
        panel.AddChild(_abilitiesText);
        _progressionText = CreateLabel(new Vector2(16, 130), new Color(0.7f, 1, 0.7f));
        panel.AddChild(_progressionText);
    }

    private static Label CreateLabel(Vector2 position, Color color)
    {
        Label label = new Label { Position = position };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static ProgressBar CreateBar(Vector2 position, Color fillColor, float maximum)
    {
        ProgressBar bar = new ProgressBar { Position = position, Size = new Vector2(588, 16), MaxValue = maximum, ShowPercentage = false };
        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.08f, 0.1f, 0.14f) });
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = fillColor });
        return bar;
    }

    private static string GetAbilityText(string key, Ability ability)
    {
        float remaining = ability.RemainingCooldown;
        string rank = $" Nv{ability.Rank}";
        return remaining <= 0.0f ? $"[{key}] PRONTA{rank}" : $"[{key}] {remaining:0.0}s{rank}";
    }
}
