using Godot;

/// <summary>Nyr'Vela passive: ability damage stores Abyss Energy; 100 energy empowers the next hit.</summary>
public partial class AbyssEnergyComponent : Node
{
    [Export] public float MaximumEnergy = 100.0f;
    [Export] public float BonusMagicDamage = 28.0f;
    public float Energy { get; private set; }
    public bool Empowered => Energy >= MaximumEnergy;
    private float _generationMultiplier = 1.0f;
    private double _generationBoostEndTime;

    public void GainFromAbility()
    {
        if (Time.GetTicksMsec() / 1000.0 >= _generationBoostEndTime) _generationMultiplier = 1.0f;
        Energy = Mathf.Min(MaximumEnergy, Energy + 20.0f * _generationMultiplier);
        Broadcast();
    }

    public float ConsumeEmpowerment()
    {
        if (!Empowered) return 0.0f;
        Energy = 0.0f;
        Broadcast();
        return BonusMagicDamage;
    }

    public void SynchronizeEnergy(float energy)
    {
        Energy = Mathf.Clamp(energy, 0.0f, MaximumEnergy);
    }

    public void ApplyGenerationBoost(float multiplier, float duration)
    {
        _generationMultiplier = Mathf.Max(_generationMultiplier, multiplier);
        _generationBoostEndTime = Mathf.Max(_generationBoostEndTime, Time.GetTicksMsec() / 1000.0 + duration);
    }

    private void Broadcast()
    {
        GetNodeOrNull<NetworkManager>("/root/NetworkManager")?.BroadcastAbyssEnergy(GetPath(), Energy);
    }
}
