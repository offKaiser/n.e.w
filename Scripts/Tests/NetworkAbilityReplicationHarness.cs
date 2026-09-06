using Godot;

/// <summary>Verifies cooldown state and presentation remain representation-only.</summary>
public partial class NetworkAbilityReplicationHarness : Node
{
    private HeroController _hero;
    private int _damageEvents;
    private int _casts;

    public override void _Ready()
    {
        _hero = new HeroController { Name = "AbilityReplica" };
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
        _hero.GetNode<AbilityController>("AbilityController").AbilityCast += (_, _, _) => _casts++;
        health.Damaged += (_, _) => _damageEvents++;

        NetworkStateReplicator.ApplyPlayerState(_hero, Vector3.Zero, Vector3.Zero, 100, 100, 100, 100, 0, 0, 1, 1, 0, 1, 1, 1, 1, 4, 0, 0, 0);
        Ability q = _hero.GetAbility("Q");
        float initialCooldown = q.RemainingCooldown;
        q.ReduceCooldown(1.0f);
        bool cooldown = initialCooldown > 3.5f && q.RemainingCooldown < initialCooldown;

        NetworkStateReplicator.ApplyPlayerState(_hero, Vector3.One, Vector3.Zero, 55, 100, 20, 100, 400, 175, 5, 2, 70, 2, 1, 1, 1, 3, 0, 0, 0);
        bool snapshot = q.RemainingCooldown > 2.5f && health.CurrentHealth == 55 && _hero.GetNode<ManaComponent>("ManaComponent").CurrentMana == 20;
        int childrenBefore = _hero.GetParent().GetChildCount();
        NetworkPresentationReplicator.PresentLocal(_hero, AbilityPresentationType.Cast, "Q", Vector3.One, 0.65f);
        bool presentation = _hero.GetParent().GetChildCount() == childrenBefore + 1 && _damageEvents == 0 && _casts == 0;
        int beforeShadow = _hero.GetParent().GetChildCount();
        NetworkPresentationReplicator.PresentLocal(_hero, AbilityPresentationType.Dash, "E", Vector3.One, 1.5f);
        bool shadow = _hero.GetParent().GetChildCount() == beforeShadow + 2;
        int beforeExplosion = _hero.GetParent().GetChildCount();
        NetworkPresentationReplicator.PresentDelayedExplosionLocal(_hero, Vector3.One, 0.5f);
        bool explosion = _hero.GetParent().GetChildCount() == beforeExplosion + 1;
        GD.Print($"[NetworkAbilityReplicationTest] cooldown={cooldown} snapshot={snapshot} presentation={presentation} shadow={shadow} explosion={explosion} singleExplosion={explosion} noGameplayReplay={_damageEvents == 0 && _casts == 0}");
    }
}
