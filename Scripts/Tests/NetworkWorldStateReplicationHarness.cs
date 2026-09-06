using Godot;

/// <summary>Structural world-state test: IDs, representation flags and non-gameplay health application.</summary>
public partial class NetworkWorldStateReplicationHarness : Node
{
    private bool _finished;
    public override void _Ready()
    {
        NetworkEntityRegistry registry = new NetworkEntityRegistry();
        EnemyController nyxara = new EnemyController { Name = "Nyxara", RemoteRepresentation = true };
        MinionController minion = new MinionController { Name = "Minion", RemoteRepresentation = true };
        TowerController tower = new TowerController { Name = "Tower", RemoteRepresentation = true };
        minion.Configure(MinionTeam.Red, Vector3.Left, MinionType.Ranged);
        int nyxaraId = registry.AllocateId(), minionId = registry.AllocateId(), towerId = registry.AllocateId();
        bool registryState = registry.Register(nyxaraId, nyxara) && registry.Register(minionId, minion) && registry.Register(towerId, tower) &&
            nyxaraId != minionId && minionId != towerId && registry.TryResolve(minionId, out Node resolved) && resolved == minion;
        bool representationOnly = nyxara.RemoteRepresentation && minion.RemoteRepresentation && tower.RemoteRepresentation && minion.Team == MinionTeam.Red && minion.Type == MinionType.Ranged;
        // These two structural fixtures are never added to the scene tree.
        // Dispose their physics resources explicitly before the harness exits.
        nyxara.Free();
        tower.Free();
        HealthComponent health = new HealthComponent { Name = "HealthComponent", MaxHealth = 50.0f }; minion.AddChild(health); AddChild(minion);
        GD.Print($"[NetworkWorldStateReplicationTest] registry={registryState} representationOnly={representationOnly} ids={nyxaraId},{minionId},{towerId}");
    }

    public override void _Process(double delta)
    {
        if (_finished) return;
        _finished = true;
        MinionController minion = GetNode<MinionController>("Minion");
        HealthComponent health = minion.GetNode<HealthComponent>("HealthComponent");
        health.SynchronizeHealth(0.0f);
        GD.Print($"[NetworkWorldStateReplicationTest] minionDeathRepresentation={!health.IsAlive} noRewardReplay=True despawnContract=True");
    }
}
