using Godot;

/// <summary>Exercises client-side state application without a network request path.</summary>
public partial class NetworkPlayerStateReplicationHarness : Node
{
    private HeroController _hero;
    private int _damageEvents;
    private int _abilityCasts;

    public override void _Ready()
    {
        _hero = new HeroController { Name = "ReplicaHero" };
        _hero.AddChild(new HealthComponent { Name = "HealthComponent", MaxHealth = 100.0f });
        _hero.AddChild(new ManaComponent { Name = "ManaComponent", MaxMana = 100.0f });
        _hero.AddChild(new GoldComponent { Name = "GoldComponent" });
        _hero.AddChild(new ProgressionComponent { Name = "ProgressionComponent" });
        _hero.AddChild(new AbyssEnergyComponent { Name = "AbyssEnergyComponent" });
        _hero.AddChild(new AbilityController { Name = "AbilityController" });
        _hero.AddChild(new DamageAbility { Name = "AbilityQ" });
        _hero.AddChild(new DamageAbility { Name = "AbilityW" });
        _hero.AddChild(new DamageAbility { Name = "AbilityE" });
        _hero.AddChild(new DamageAbility { Name = "AbilityR" });
        AddChild(_hero);
        CallDeferred(nameof(Run));
    }

    private void Run()
    {
        HealthComponent health = _hero.GetNode<HealthComponent>("HealthComponent");
        AbilityController abilities = _hero.GetNode<AbilityController>("AbilityController");
        health.Damaged += (_, _) => _damageEvents++;
        abilities.AbilityCast += (_, _, _) => _abilityCasts++;

        NetworkStateReplicator.ApplyPlayerState(_hero, new Vector3(4, 0, -3), new Vector3(0, 1, 0), 70, 100, 60, 100, 100, 250, 5, 2, 70, 2, 1, 1, 1);
        bool state = health.CurrentHealth == 70 && _hero.GetNode<ManaComponent>("ManaComponent").CurrentMana == 60 &&
            _hero.GetNode<GoldComponent>("GoldComponent").Gold == 100 && _hero.GetNode<ProgressionComponent>("ProgressionComponent").Level == 5 &&
            _hero.GetNode<ProgressionComponent>("ProgressionComponent").SkillPoints == 2 && _hero.GetNode<AbyssEnergyComponent>("AbyssEnergyComponent").Energy == 70 &&
            _hero.GetAbility("Q").Rank == 2 && _hero.GlobalPosition == new Vector3(4, 0, -3);
        bool noGameplayReplay = _damageEvents == 0 && _abilityCasts == 0;

        NetworkStateReplicator.ApplyPlayerState(_hero, Vector3.One, Vector3.Zero, 55, 100, 20, 100, 400, 175, 5, 2, 70, 2, 1, 1, 1);
        bool snapshot = health.CurrentHealth == 55 && _hero.GetNode<ManaComponent>("ManaComponent").CurrentMana == 20 &&
            _hero.GetNode<GoldComponent>("GoldComponent").Gold == 400 && _hero.GetNode<ProgressionComponent>("ProgressionComponent").Level == 5;
        GD.Print($"[PlayerStateReplicationTest] state={state} snapshot={snapshot} noGameplayReplay={noGameplayReplay}");
    }
}
