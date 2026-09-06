using Godot;

/// <summary>Stable session-local identity for a replicated entity. Zero is invalid.</summary>
public partial class NetworkIdentity : Node
{
    [Export] public int NetworkEntityId { get; private set; }
    public bool IsValid => NetworkEntityId > 0;
    public bool Assign(int entityId)
    {
        if (entityId <= 0 || (IsValid && NetworkEntityId != entityId)) return false;
        NetworkEntityId = entityId;
        return true;
    }
}
