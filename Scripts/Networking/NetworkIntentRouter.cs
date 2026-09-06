using System;
using Godot;

/// <summary>
/// Server-side gateway for player commands. The actor is always resolved from
/// the RPC sender; request payloads contain only the requested action.
/// </summary>
public partial class NetworkIntentRouter : Node
{
    private NetworkManager _network;

    public override void _Ready()
    {
        _network = GetParent() as NetworkManager;
    }

    public void RequestMove(Vector3 destination)
    {
        if (_network == null || !_network.SessionActive) return;
        if (_network.IsServer) ExecuteMoveFromPeer(_network.Multiplayer.GetUniqueId(), destination);
        else RpcId(1, nameof(ReceiveMoveIntent), destination);
    }

    public void RequestBasicAttack(int targetEntityId)
    {
        if (_network == null || !_network.SessionActive) return;
        if (_network.IsServer) ExecuteBasicAttackFromPeer(_network.Multiplayer.GetUniqueId(), targetEntityId);
        else RpcId(1, nameof(ReceiveBasicAttackIntent), targetEntityId);
    }

    public void RequestAbilityCast(string slot, int targetEntityId, Vector3 targetPosition, bool hasTarget, bool hasPosition)
    {
        if (_network == null || !_network.SessionActive) return;
        if (_network.IsServer) ExecuteAbilityCastFromPeer(_network.Multiplayer.GetUniqueId(), slot, targetEntityId, targetPosition, hasTarget, hasPosition);
        else RpcId(1, nameof(ReceiveAbilityCastIntent), slot, targetEntityId, targetPosition, hasTarget, hasPosition);
    }

    public void RequestAbilityUpgrade(string slot)
    {
        if (_network == null || !_network.SessionActive) return;
        if (_network.IsServer) ExecuteAbilityUpgradeFromPeer(_network.Multiplayer.GetUniqueId(), slot);
        else RpcId(1, nameof(ReceiveAbilityUpgradeIntent), slot);
    }

    // Move is intentionally unreliable because a newer destination supersedes an older one.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveMoveIntent(Vector3 destination)
    {
        if (_network?.IsServer != true) return;
        ExecuteMoveFromPeer(Multiplayer.GetRemoteSenderId(), destination);
    }

    // Attack, cast and upgrade use Godot's default reliable transfer mode.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ReceiveBasicAttackIntent(int targetEntityId)
    {
        if (_network?.IsServer != true) return;
        ExecuteBasicAttackFromPeer(Multiplayer.GetRemoteSenderId(), targetEntityId);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ReceiveAbilityCastIntent(string slot, int targetEntityId, Vector3 targetPosition, bool hasTarget, bool hasPosition)
    {
        if (_network?.IsServer != true) return;
        ExecuteAbilityCastFromPeer(Multiplayer.GetRemoteSenderId(), slot, targetEntityId, targetPosition, hasTarget, hasPosition);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ReceiveAbilityUpgradeIntent(string slot)
    {
        if (_network?.IsServer != true) return;
        ExecuteAbilityUpgradeFromPeer(Multiplayer.GetRemoteSenderId(), slot);
    }

    private void ExecuteMoveFromPeer(long peerId, Vector3 destination)
    {
        if (!IsFinite(destination) || !_network.TryResolveControlledEntity(peerId, out HeroController hero) || !IsAlive(hero)) return;
        hero.ApplyMoveIntent(destination);
    }

    private void ExecuteBasicAttackFromPeer(long peerId, int targetEntityId)
    {
        if (!_network.TryResolveControlledEntity(peerId, out HeroController hero) ||
            !TryResolveEnemyTarget(hero, targetEntityId, out HealthComponent target)) return;
        hero.ApplyBasicAttackIntent(target);
    }

    private void ExecuteAbilityCastFromPeer(long peerId, string slot, int targetEntityId, Vector3 targetPosition, bool hasTarget, bool hasPosition)
    {
        if (!Enum.TryParse(slot, true, out AbilitySlot _) || !IsFinite(targetPosition) ||
            !_network.TryResolveControlledEntity(peerId, out HeroController hero) || !IsAlive(hero)) return;

        HealthComponent target = null;
        if (hasTarget && !TryResolveEnemyTarget(hero, targetEntityId, out target)) return;
        if (!hasTarget && targetEntityId != 0) return;
        if (!hasPosition) targetPosition = hero.GlobalPosition;

        if (hero.TryCastAbilityLocal(slot, target))
            _network.PresentationReplicator.PublishAbilityCast(hero, slot, target);
    }

    private void ExecuteAbilityUpgradeFromPeer(long peerId, string slot)
    {
        if (!Enum.TryParse(slot, true, out AbilitySlot _) ||
            !_network.TryResolveControlledEntity(peerId, out HeroController hero) || !IsAlive(hero)) return;
        hero.TryIncreaseAbilityLocal(slot);
    }

    private bool TryResolveEnemyTarget(HeroController actor, int targetEntityId, out HealthComponent target)
    {
        target = null;
        if (targetEntityId <= 0 || !_network.EntityRegistry.TryResolve(targetEntityId, out Node node) ||
            node == actor || node is not Node3D targetNode || !CombatTeams.IsEnemy(actor, targetNode)) return false;
        target = targetNode.GetNodeOrNull<HealthComponent>("HealthComponent");
        return target != null && target.IsAlive;
    }

    private static bool IsAlive(Node node)
    {
        HealthComponent health = node.GetNodeOrNull<HealthComponent>("HealthComponent");
        return health == null || health.IsAlive;
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
