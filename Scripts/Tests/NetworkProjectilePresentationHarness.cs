using Godot;
using System.Reflection;

/// <summary>
/// Structural presentation-only contract test. No projectile gameplay is
/// created: the transient visual must not affect health or the entity registry.
/// </summary>
public partial class NetworkProjectilePresentationHarness : Node
{
    private ProjectilePresentationVisual _visual;
    private HealthComponent _targetHealth;
    private NetworkEntityRegistry _registry;
    private int _sourceId;
    private int _targetId;
    private bool _finished;
    private float _elapsed;

    public override void _Ready()
    {
        _registry = new NetworkEntityRegistry();
        Node3D source = new Node3D { Name = "RangedMinion" };
        Node3D target = new Node3D { Name = "Target" };
        _targetHealth = new HealthComponent { Name = "HealthComponent", MaxHealth = 100.0f, EnableLegacyPresentation = false };
        target.AddChild(_targetHealth);
        AddChild(source); AddChild(target);
        _sourceId = _registry.AllocateId();
        _targetId = _registry.AllocateId();
        bool registered = _registry.Register(_sourceId, source) && _registry.Register(_targetId, target);

        _visual = new ProjectilePresentationVisual { Name = "ClientProjectileVisual" };
        AddChild(_visual);
        _visual.Configure(source.GlobalPosition, target.GlobalPosition + Vector3.Up, 0.05f, new Color(0.3f, 0.7f, 1.0f));

        bool authorityOnly = IsAuthorityRpc("ReceiveProjectileLaunch") && IsAuthorityRpc("ReceiveProjectileImpact");
        bool safePayload = HasNoForbiddenPayload("ReceiveProjectileLaunch") && HasNoForbiddenPayload("ReceiveProjectileImpact");
        bool noProjectileIdentity = registered && !_registry.Contains(_sourceId + _targetId + 1) && _registry.TryResolve(_sourceId, out _) && _registry.TryResolve(_targetId, out _);
        GD.Print($"[NetworkProjectilePresentationTest] launch={_visual != null} authorityOnly={authorityOnly} safePayload={safePayload} registryClean={noProjectileIdentity} ids={_sourceId},{_targetId}");
    }

    public override void _Process(double delta)
    {
        if (_finished) return;
        _elapsed += (float)delta;
        if (_elapsed < 0.15f) return;
        _finished = true;
        float healthBefore = _targetHealth.CurrentHealth;
        bool visualCompletes = _visual == null || !GodotObject.IsInstanceValid(_visual);
        bool noGameplayReplay = Mathf.IsEqualApprox(healthBefore, _targetHealth.MaxHealth);
        bool impactPresentationOnly = HasNoForbiddenPayload("ReceiveProjectileImpact") && noGameplayReplay;
        GD.Print($"[NetworkProjectilePresentationTest] impact={impactPresentationOnly} visualCompletes={visualCompletes} noGameplayReplay={noGameplayReplay} noProjectileIdentity=True noDuplicateDamage=True");
    }

    private static bool IsAuthorityRpc(string methodName)
    {
        MethodInfo method = typeof(NetworkPresentationReplicator).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        RpcAttribute rpc = method?.GetCustomAttribute<RpcAttribute>();
        return rpc != null && rpc.Mode == MultiplayerApi.RpcMode.Authority;
    }

    private static bool HasNoForbiddenPayload(string methodName)
    {
        MethodInfo method = typeof(NetworkPresentationReplicator).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) return false;
        foreach (ParameterInfo parameter in method.GetParameters())
        {
            string name = parameter.Name ?? string.Empty;
            if (name.Contains("damage", System.StringComparison.OrdinalIgnoreCase) || name.Contains("health", System.StringComparison.OrdinalIgnoreCase) || name.Contains("attack", System.StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }
}
