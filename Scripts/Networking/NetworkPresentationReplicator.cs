using System.Collections.Generic;
using Godot;

public enum AbilityPresentationType { Cast, Beam, Dash, FieldStart, DelayedShadowExplosion, ProjectileLaunch, ProjectileImpact }
public enum ProjectileVisualType { MinionRanged, TowerShot, NyxaraShot, GenericMagic }

/// <summary>Authority-to-client transient ability visuals. Never executes gameplay.</summary>
public partial class NetworkPresentationReplicator : Node
{
    private NetworkManager _network;
    private readonly HashSet<int> _unknownCasterWarnings = new();
    private readonly Dictionary<int, Node3D> _dashShadows = new();

    public override void _Ready() => _network = GetParent() as NetworkManager;

    public void PublishAbilityCast(HeroController caster, string slot, HealthComponent target)
    {
        if (_network?.IsServer != true) return;
        int casterId = _network.GetEntityId(caster);
        int targetId = target == null ? 0 : _network.GetEntityId(target.GetParent());
        if (casterId <= 0) return;
        Vector3 position = target?.GetParent<Node3D>().GlobalPosition ?? caster.GlobalPosition;
        AbilityPresentationType type = slot == "W" ? AbilityPresentationType.Beam : slot == "E" ? AbilityPresentationType.Dash : slot == "R" ? AbilityPresentationType.FieldStart : AbilityPresentationType.Cast;
        float duration = slot == "R" ? 5.0f : slot == "E" ? 1.5f : 0.65f;
        Rpc(nameof(ReceiveAbilityPresentation), casterId, (int)type, slot, targetId, position, target != null, duration);
    }

    public void PublishDelayedShadowExplosion(Node3D caster, Vector3 position, float duration)
    {
        if (_network?.IsServer != true) return;
        int casterId = _network.GetEntityId(caster);
        if (casterId > 0)
            Rpc(nameof(ReceiveAbilityPresentation), casterId, (int)AbilityPresentationType.DelayedShadowExplosion, "E", 0, position, false, duration);
    }

    /// <summary>
    /// Boundary used by gameplay-owned delayed effects. The lookup stays in the
    /// presentation layer, so gameplay does not depend on NetworkManager.
    /// </summary>
    public static void RequestDelayedShadowExplosionPresentation(Node3D caster, Vector3 position, float duration)
    {
        NetworkManager network = GetNetwork(caster);
        network?.PresentationReplicator?.PublishDelayedShadowExplosion(caster, position, duration);
    }

    public static void PublishProjectileLaunch(Node3D source, Node3D target, ProjectileVisualType visualType, float speed, Color color)
    {
        NetworkManager network = GetNetwork(source);
        if (network?.IsServer != true) return;
        int sourceId = network.GetEntityId(source); int targetId = network.GetEntityId(target);
        if (sourceId <= 0) return;
        Vector3 start = source.GlobalPosition + Vector3.Up * 0.85f;
        Vector3 end = target.GlobalPosition + Vector3.Up * 0.9f;
        float duration = Mathf.Max(start.DistanceTo(end) / Mathf.Max(speed, 1.0f), 0.05f);
        network.PresentationReplicator.Rpc(nameof(ReceiveProjectileLaunch), sourceId, targetId, start, end, (int)visualType, duration, color);
    }

    public static void PublishProjectileImpact(Node3D source, Node3D target, ProjectileVisualType visualType, Vector3 position)
    {
        NetworkManager network = GetNetwork(source);
        if (network?.IsServer != true) return;
        int sourceId = network.GetEntityId(source); int targetId = network.GetEntityId(target);
        if (sourceId > 0) network.PresentationReplicator.Rpc(nameof(ReceiveProjectileImpact), sourceId, targetId, position, (int)visualType);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ReceiveAbilityPresentation(int casterId, int typeValue, string slot, int targetId, Vector3 position, bool hasTarget, float duration)
    {
        if (!System.Enum.IsDefined(typeof(AbilityPresentationType), typeValue) || !TryResolveCaster(casterId, out HeroController caster)) return;
        AbilityPresentationType type = (AbilityPresentationType)typeValue;
        if (type == AbilityPresentationType.DelayedShadowExplosion)
        {
            if (_dashShadows.Remove(casterId, out Node3D shadow) && GodotObject.IsInstanceValid(shadow)) shadow.QueueFree();
            PresentDelayedExplosionLocal(caster, position, duration);
            return;
        }
        Node3D spawnedShadow = PresentLocal(caster, type, slot, position, duration);
        if (type == AbilityPresentationType.Dash && spawnedShadow != null) _dashShadows[casterId] = spawnedShadow;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ReceiveProjectileLaunch(int sourceId, int targetId, Vector3 start, Vector3 end, int visualValue, float duration, Color color)
    {
        if (!System.Enum.IsDefined(typeof(ProjectileVisualType), visualValue) || !TryResolvePresentationEntity(sourceId, out Node3D _)) return;
        ProjectilePresentationVisual visual = new ProjectilePresentationVisual();
        GetTree().CurrentScene?.AddChild(visual); visual.Configure(start, end, duration, color);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ReceiveProjectileImpact(int sourceId, int targetId, Vector3 position, int visualValue)
    {
        if (!System.Enum.IsDefined(typeof(ProjectileVisualType), visualValue) || !TryResolvePresentationEntity(sourceId, out Node3D _)) return;
        TimedVfx impact = new TimedVfx(); GetTree().CurrentScene?.AddChild(impact);
        impact.GlobalPosition = position; impact.Configure(new Color(0.7f, 0.25f, 1.0f), 0.65f, 0.3f);
    }

    public static Node3D PresentLocal(HeroController caster, AbilityPresentationType type, string slot, Vector3 position, float duration)
    {
        Node parent = caster?.GetParent();
        if (parent == null) return null;
        Color color = new Color(0.42f, 0.05f, 0.95f);
        if (type == AbilityPresentationType.Beam)
        {
            BeamVfx beam = new BeamVfx(); parent.AddChild(beam);
            beam.Configure(caster.GlobalPosition + Vector3.Up, position + Vector3.Up, color, 0.09f, 0.38f);
            return null;
        }
        TimedVfx vfx = new TimedVfx(); parent.AddChild(vfx);
        vfx.GlobalPosition = position + Vector3.Up * 0.35f;
        float radius = type == AbilityPresentationType.FieldStart ? 5.0f : type == AbilityPresentationType.Dash ? 2.0f : slot == "Q" ? 3.0f : 1.8f;
        vfx.Configure(color, radius, duration);
        if (type == AbilityPresentationType.Dash)
        {
            TimedVfx shadow = new TimedVfx(); parent.AddChild(shadow);
            shadow.GlobalPosition = caster.GlobalPosition + Vector3.Up * 0.3f;
            shadow.Configure(color, 2.8f, duration);
            return shadow;
        }
        return null;
    }

    public static void PresentDelayedExplosionLocal(HeroController caster, Vector3 position, float duration)
    {
        Node parent = caster?.GetParent();
        if (parent == null) return;
        TimedVfx burst = new TimedVfx(); parent.AddChild(burst);
        burst.GlobalPosition = position + Vector3.Up * 0.3f;
        burst.Configure(new Color(0.47f, 0.05f, 1.0f), 2.8f, duration);
    }

    private bool TryResolveCaster(int id, out HeroController caster)
    {
        caster = null;
        if (_network?.EntityRegistry.TryResolve(id, out Node node) == true && (caster = node as HeroController) != null) return true;
        if (_unknownCasterWarnings.Add(id)) GD.PushWarning($"[PresentationReplicator] Caster {id} ainda nao existe localmente.");
        return false;
    }

    private bool TryResolvePresentationEntity(int id, out Node3D entity)
    {
        entity = null;
        return _network?.EntityRegistry.TryResolve(id, out Node node) == true && (entity = node as Node3D) != null;
    }

    private static NetworkManager GetNetwork(Node node) => node?.GetNodeOrNull<NetworkManager>("/root/NetworkManager");
}
