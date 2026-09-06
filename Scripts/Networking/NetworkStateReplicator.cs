using System.Collections.Generic;
using Godot;

/// <summary>
/// Host-to-client replication for authoritative player presentation state.
/// It observes gameplay components but never decides gameplay rules.
/// </summary>
public partial class NetworkStateReplicator : Node
{
    [Export(PropertyHint.Range, "10,20,1")]
    public float TransformRateHz = 15.0f;

    private NetworkManager _network;
    private readonly Dictionary<int, HeroController> _players = new();
    private readonly Dictionary<int, Node3D> _world = new();
    private readonly HashSet<int> _unknownEntityWarnings = new();
    private double _nextTransformBroadcast;

    public override void _Ready() => _network = GetParent() as NetworkManager;

    public override void _PhysicsProcess(double delta)
    {
        if (_network?.IsServer != true || Time.GetTicksMsec() / 1000.0 < _nextTransformBroadcast) return;
        _nextTransformBroadcast = Time.GetTicksMsec() / 1000.0 + 1.0 / Mathf.Max(TransformRateHz, 1.0f);
        foreach ((int entityId, HeroController hero) in _players)
        {
            if (GodotObject.IsInstanceValid(hero))
            {
                Rpc(nameof(ReceivePlayerTransformState), entityId, hero.GlobalPosition, hero.Rotation);
                PublishCooldowns(entityId, hero);
            }
        }
        foreach ((int entityId, Node3D entity) in _world)
        {
            if (GodotObject.IsInstanceValid(entity) && entity is not TowerController)
                Rpc(nameof(ReceiveWorldTransformState), entityId, entity.GlobalPosition, entity.Rotation);
        }
    }

    public void ObserveAuthoritativePlayer(HeroController hero, int entityId)
    {
        if (_network?.IsServer != true || entityId <= 0 || hero == null || _players.ContainsKey(entityId)) return;
        _players.Add(entityId, hero);
        HealthComponent health = hero.GetNodeOrNull<HealthComponent>("HealthComponent");
        ManaComponent mana = hero.GetNodeOrNull<ManaComponent>("ManaComponent");
        GoldComponent gold = hero.GetNodeOrNull<GoldComponent>("GoldComponent");
        ProgressionComponent progression = hero.GetNodeOrNull<ProgressionComponent>("ProgressionComponent");
        AbyssEnergyComponent abyss = hero.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent");
        if (health != null) health.HealthChanged += (current, maximum) => PublishHealth(entityId, current, maximum);
        if (mana != null) mana.ManaChanged += (current, maximum) => PublishMana(entityId, current, maximum);
        if (gold != null) gold.GoldChanged += _ => PublishGold(entityId, gold.Gold);
        if (progression != null)
        {
            progression.ExperienceChanged += (_, _) => PublishProgression(entityId, progression);
            progression.LevelChanged += _ => PublishProgression(entityId, progression);
            progression.SkillPointsChanged += _ => PublishProgression(entityId, progression);
        }
        if (abyss != null) abyss.EnergyChanged += (_, _) => PublishAbyss(entityId, abyss.Energy);
        foreach (string slot in new[] { "Q", "W", "E", "R" })
        {
            Ability ability = hero.GetAbility(slot);
            if (ability != null)
            {
                ability.RankChanged += _ => PublishRanks(entityId, hero);
                ability.CooldownChanged += _ => PublishCooldowns(entityId, hero);
            }
        }
    }

    public void SendPlayerSnapshots(long peerId)
    {
        if (_network?.IsServer != true) return;
        foreach ((int entityId, HeroController hero) in _players)
        {
            if (GodotObject.IsInstanceValid(hero)) SendSnapshot(peerId, entityId, hero);
        }
    }

    public void ForgetAuthoritativePlayer(int entityId) => _players.Remove(entityId);

    public void ObserveAuthoritativeWorld(Node3D entity, int entityId)
    {
        if (_network?.IsServer != true || entityId <= 0 || entity == null || _world.ContainsKey(entityId)) return;
        _world.Add(entityId, entity);
        HealthComponent health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health != null)
        {
            health.HealthChanged += (current, maximum) => Rpc(nameof(ReceiveWorldHealthState), entityId, current, maximum);
            health.Died += _ => OnWorldDied(entityId, entity);
        }
    }

    public void PublishMinionSpawn(MinionController minion, int entityId)
    {
        if (_network?.IsServer != true) return;
        Rpc(nameof(ReceiveMinionSpawn), entityId, (int)minion.Team, (int)minion.Type, minion.LaneDirection, minion.GlobalPosition);
    }

    public void SendWorldSnapshots(long peerId)
    {
        if (_network?.IsServer != true) return;
        foreach ((int id, Node3D entity) in _world)
        {
            HealthComponent health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (entity is MinionController minion)
                RpcId(peerId, nameof(ReceiveMinionSpawn), id, (int)minion.Team, (int)minion.Type, minion.LaneDirection, minion.GlobalPosition);
            RpcId(peerId, nameof(ReceiveWorldTransformState), id, entity.GlobalPosition, entity.Rotation);
            if (health != null) RpcId(peerId, nameof(ReceiveWorldHealthState), id, health.CurrentHealth, health.MaxHealth);
        }
    }

    private void SendSnapshot(long peerId, int entityId, HeroController hero)
    {
        HealthComponent health = hero.GetNodeOrNull<HealthComponent>("HealthComponent");
        ManaComponent mana = hero.GetNodeOrNull<ManaComponent>("ManaComponent");
        GoldComponent gold = hero.GetNodeOrNull<GoldComponent>("GoldComponent");
        ProgressionComponent progression = hero.GetNodeOrNull<ProgressionComponent>("ProgressionComponent");
        AbyssEnergyComponent abyss = hero.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent");
        RpcId(peerId, nameof(ReceivePlayerSnapshot), entityId, hero.GlobalPosition, hero.Rotation,
            health?.CurrentHealth ?? 0.0f, health?.MaxHealth ?? 0.0f,
            mana?.CurrentMana ?? 0.0f, mana?.MaxMana ?? 0.0f,
            gold?.Gold ?? 0, progression?.Experience ?? 0, progression?.Level ?? 1, progression?.SkillPoints ?? 0,
            abyss?.Energy ?? 0.0f, GetRank(hero, "Q"), GetRank(hero, "W"), GetRank(hero, "E"), GetRank(hero, "R"),
            GetCooldown(hero, "Q"), GetCooldown(hero, "W"), GetCooldown(hero, "E"), GetCooldown(hero, "R"));
    }

    private void PublishHealth(int id, float current, float maximum) { if (_network.IsServer) Rpc(nameof(ReceiveHealthState), id, current, maximum); }
    private void PublishMana(int id, float current, float maximum) { if (_network.IsServer) Rpc(nameof(ReceiveManaState), id, current, maximum); }
    private void PublishGold(int id, int gold) { if (_network.IsServer) Rpc(nameof(ReceiveGoldState), id, gold); }
    private void PublishProgression(int id, ProgressionComponent p) { if (_network.IsServer) Rpc(nameof(ReceiveProgressionState), id, p.Experience, p.Level, p.SkillPoints); }
    private void PublishRanks(int id, HeroController hero) { if (_network.IsServer) Rpc(nameof(ReceiveAbilityRanksState), id, GetRank(hero, "Q"), GetRank(hero, "W"), GetRank(hero, "E"), GetRank(hero, "R")); }
    private void PublishCooldowns(int id, HeroController hero) { if (_network.IsServer) Rpc(nameof(ReceiveAbilityCooldownsState), id, GetCooldown(hero, "Q"), GetCooldown(hero, "W"), GetCooldown(hero, "E"), GetCooldown(hero, "R")); }
    private void PublishAbyss(int id, float energy) { if (_network.IsServer) Rpc(nameof(ReceiveAbyssEnergyState), id, energy); }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ReceivePlayerSnapshot(int id, Vector3 position, Vector3 rotation, float health, float maxHealth, float mana, float maxMana, int gold, int experience, int level, int skillPoints, float abyss, int q, int w, int e, int r, float qCooldown, float wCooldown, float eCooldown, float rCooldown)
    {
        if (TryResolvePlayer(id, out HeroController hero)) ApplyPlayerState(hero, position, rotation, health, maxHealth, mana, maxMana, gold, experience, level, skillPoints, abyss, q, w, e, r, qCooldown, wCooldown, eCooldown, rCooldown);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceivePlayerTransformState(int id, Vector3 position, Vector3 rotation) { if (TryResolvePlayer(id, out HeroController hero)) hero.ApplyNetworkTransform(position, rotation); }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveHealthState(int id, float current, float maximum) { if (TryResolvePlayer(id, out HeroController hero)) ApplyHealth(hero, current, maximum); }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveManaState(int id, float current, float maximum) { if (TryResolvePlayer(id, out HeroController hero)) ApplyMana(hero, current, maximum); }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveGoldState(int id, int gold) { if (TryResolvePlayer(id, out HeroController hero)) hero.GetNodeOrNull<GoldComponent>("GoldComponent")?.SynchronizeGold(gold); }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveProgressionState(int id, int experience, int level, int skillPoints) { if (TryResolvePlayer(id, out HeroController hero)) hero.GetNodeOrNull<ProgressionComponent>("ProgressionComponent")?.SynchronizeProgression(level, experience, skillPoints); }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveAbilityRanksState(int id, int q, int w, int e, int r) { if (TryResolvePlayer(id, out HeroController hero)) ApplyRanks(hero, q, w, e, r); }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveAbilityCooldownsState(int id, float q, float w, float e, float r) { if (TryResolvePlayer(id, out HeroController hero)) ApplyCooldowns(hero, q, w, e, r); }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveAbyssEnergyState(int id, float energy) { if (TryResolvePlayer(id, out HeroController hero)) hero.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent")?.SynchronizeEnergy(energy); }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveWorldTransformState(int id, Vector3 position, Vector3 rotation) { if (TryResolveWorld(id, out Node3D entity)) { entity.GlobalPosition = position; entity.Rotation = rotation; } }
    [Rpc(MultiplayerApi.RpcMode.Authority)] private void ReceiveWorldHealthState(int id, float current, float maximum) { if (TryResolveWorld(id, out Node3D entity)) { HealthComponent health = entity.GetNodeOrNull<HealthComponent>("HealthComponent"); if (health != null) { health.SetMaxHealth(maximum, false); health.SynchronizeHealth(current); } } }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ReceiveMinionSpawn(int id, int team, int type, Vector3 laneDirection, Vector3 position)
    {
        if (_network?.EntityRegistry.Contains(id) == true) return;
        Node scene = GetTree().CurrentScene; if (scene == null || id <= 0) return;
        PackedScene packed = GD.Load<PackedScene>("res://Scenes/Units/Minion.tscn");
        MinionController minion = packed.Instantiate<MinionController>();
        minion.Configure((MinionTeam)team, laneDirection, (MinionType)type); minion.RemoteRepresentation = true;
        scene.AddChild(minion); minion.GlobalPosition = position;
        _network.RegisterReplicaWorldEntity(minion, id);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ReceiveWorldDespawn(int id)
    {
        if (_network?.EntityRegistry.TryResolve(id, out Node node) == true) node.QueueFree();
        _network?.EntityRegistry.Unregister(id);
    }

    public static void ApplyPlayerState(HeroController hero, Vector3 position, Vector3 rotation, float health, float maxHealth, float mana, float maxMana, int gold, int experience, int level, int skillPoints, float abyss, int q, int w, int e, int r, float qCooldown = 0.0f, float wCooldown = 0.0f, float eCooldown = 0.0f, float rCooldown = 0.0f)
    {
        hero.ApplyNetworkTransform(position, rotation);
        ApplyHealth(hero, health, maxHealth); ApplyMana(hero, mana, maxMana);
        hero.GetNodeOrNull<GoldComponent>("GoldComponent")?.SynchronizeGold(gold);
        hero.GetNodeOrNull<ProgressionComponent>("ProgressionComponent")?.SynchronizeProgression(level, experience, skillPoints);
        hero.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent")?.SynchronizeEnergy(abyss);
        ApplyRanks(hero, q, w, e, r);
        ApplyCooldowns(hero, qCooldown, wCooldown, eCooldown, rCooldown);
    }

    private bool TryResolvePlayer(int id, out HeroController hero)
    {
        hero = null;
        if (_network?.EntityRegistry.TryResolve(id, out Node node) == true && (hero = node as HeroController) != null) return true;
        if (_unknownEntityWarnings.Add(id)) GD.PushWarning($"[StateReplicator] Estado ignorado: entidade {id} ainda nao existe localmente.");
        return false;
    }

    private bool TryResolveWorld(int id, out Node3D entity)
    {
        entity = null;
        return _network?.EntityRegistry.TryResolve(id, out Node node) == true && (entity = node as Node3D) != null;
    }

    private void OnWorldDied(int id, Node3D entity)
    {
        if (_network?.IsServer != true || entity is not MinionController) return;
        Rpc(nameof(ReceiveWorldDespawn), id);
        _world.Remove(id); _network.EntityRegistry.Unregister(id); entity.QueueFree();
    }

    private static void ApplyHealth(HeroController hero, float current, float maximum)
    {
        HealthComponent health = hero.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health == null) return;
        if (maximum > 0.0f) health.SetMaxHealth(maximum, false);
        health.SynchronizeHealth(current);
    }
    private static void ApplyMana(HeroController hero, float current, float maximum)
    {
        ManaComponent mana = hero.GetNodeOrNull<ManaComponent>("ManaComponent");
        if (mana == null) return;
        if (maximum > 0.0f) mana.SetMaxMana(maximum, false);
        mana.SynchronizeMana(current);
    }
    private static void ApplyRanks(HeroController hero, int q, int w, int e, int r)
    {
        hero.GetAbility("Q")?.SynchronizeRank(q); hero.GetAbility("W")?.SynchronizeRank(w);
        hero.GetAbility("E")?.SynchronizeRank(e); hero.GetAbility("R")?.SynchronizeRank(r);
    }
    private static void ApplyCooldowns(HeroController hero, float q, float w, float e, float r)
    {
        hero.GetAbility("Q")?.SynchronizeCooldown(q); hero.GetAbility("W")?.SynchronizeCooldown(w);
        hero.GetAbility("E")?.SynchronizeCooldown(e); hero.GetAbility("R")?.SynchronizeCooldown(r);
    }
    private static int GetRank(HeroController hero, string slot) => hero.GetAbility(slot)?.Rank ?? 1;
    private static float GetCooldown(HeroController hero, string slot) => hero.GetAbility(slot)?.RemainingCooldown ?? 0.0f;
}
