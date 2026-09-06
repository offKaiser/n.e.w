using Godot;

public partial class MinionSpawner : Node3D
{
    [Export]
    public PackedScene MinionScene;

    [Export]
    public MinionTeam Team = MinionTeam.Blue;

    [Export]
    public Vector3 LaneDirection = Vector3.Right;

    [Export]
    public float SpawnInterval = 12.0f;

    private double _nextSpawnTime;

    public override void _Ready()
    {
        if (IsClientReplica()) return;
        _nextSpawnTime = Time.GetTicksMsec() / 1000.0 + SpawnInterval;
        CallDeferred(nameof(SpawnInitialWave));
    }

    public override void _Process(double delta)
    {
        if (IsClientReplica()) return;
        if (Time.GetTicksMsec() / 1000.0 < _nextSpawnTime)
        {
            return;
        }

        SpawnWave();
        _nextSpawnTime = Time.GetTicksMsec() / 1000.0 + SpawnInterval;
    }

    private void SpawnWave()
    {
        if (MinionScene == null || GetParent() == null)
        {
            return;
        }

        MinionType[] composition =
        {
            MinionType.Melee, MinionType.Melee, MinionType.Melee, MinionType.Tank,
            MinionType.Ranged, MinionType.Ranged, MinionType.Ranged
        };

        for (int index = 0; index < composition.Length; index++)
        {
            MinionController minion = MinionScene.Instantiate<MinionController>();
            minion.Configure(Team, LaneDirection, composition[index]);
            NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
            if (network != null && network.SessionActive && network.IsServer)
                minion.Name = network.AllocateMinionName();
            GetParent().AddChild(minion);

            float sideOffset = (index % 3 - 1) * 1.4f;
            float rowOffset = (index / 3) * 1.8f;
            minion.GlobalPosition = GlobalPosition + Vector3.Forward * sideOffset - LaneDirection * rowOffset;
            if (network != null && network.SessionActive && network.IsServer)
                network.RegisterAuthoritativeMinion(minion);
        }
    }

    private void SpawnInitialWave()
    {
        if (!IsClientReplica()) SpawnWave();
    }

    private bool IsClientReplica()
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        return network != null && network.SessionActive && !network.IsServer;
    }
}
