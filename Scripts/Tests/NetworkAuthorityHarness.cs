using Godot;
using System.Reflection;

/// <summary>Offline structural check for the identity + ownership part of intent routing.</summary>
public partial class NetworkAuthorityHarness : Node
{
    public override void _Ready()
    {
        NetworkEntityRegistry registry = new NetworkEntityRegistry();
        NetworkPeerOwnership ownership = new NetworkPeerOwnership();
        Node host = new Node { Name = "HostPlayer" };
        Node client = new Node { Name = "ClientPlayer" };
        int hostId = registry.AllocateId();
        int clientId = registry.AllocateId();
        bool registered = registry.Register(hostId, host) && registry.Register(clientId, client);
        bool owned = ownership.Assign(1, hostId) && ownership.Assign(2, clientId);
        bool resolved = ownership.TryGetControlledEntityId(1, out int resolvedHost) && resolvedHost == hostId &&
                        ownership.TryGetControlledEntityId(2, out int resolvedClient) && resolvedClient == clientId &&
                        registry.TryResolve(resolvedHost, out Node hostNode) && hostNode == host &&
                        registry.TryResolve(resolvedClient, out Node clientNode) && clientNode == client;
        bool rejected = !ownership.Assign(2, hostId) && !ownership.Assign(3, clientId) && !ownership.TryGetControlledEntityId(99, out _);
        bool routerSignatures = HasIntent("ReceiveMoveIntent", typeof(Vector3)) &&
            HasIntent("ReceiveBasicAttackIntent", typeof(int)) &&
            HasIntent("ReceiveAbilityCastIntent", typeof(string), typeof(int), typeof(Vector3), typeof(bool), typeof(bool)) &&
            HasIntent("ReceiveAbilityUpgradeIntent", typeof(string));
        bool legacyRemoved = HasNoNetworkManagerMethod("RequestDamage") &&
            HasNoNetworkManagerMethod("RequestManaSpend") &&
            HasNoNetworkManagerMethod("RequestAbilityCast") &&
            HasNoNetworkManagerMethod("RequestAbilityUpgrade") &&
            HasNoNetworkManagerMethod("ApplyDamageRequest") &&
            HasNoNetworkManagerMethod("ApplyManaSpendRequest") &&
            HasNoNetworkManagerMethod("ApplyAbilityCast") &&
            HasNoNetworkManagerMethod("ApplyAbilityUpgrade") &&
            HasNoNetworkManagerMethod("PublishPlayerTransform");
        bool payloadsSafe = HasNoPayloadParameter("ReceiveMoveIntent", "player", "entity", "attacker", "caster") &&
            HasNoPayloadParameter("ReceiveBasicAttackIntent", "attacker", "caster", "source", "damage") &&
            HasNoPayloadParameter("ReceiveAbilityCastIntent", "caster", "hero", "source", "mana") &&
            HasNoPayloadParameter("ReceiveAbilityUpgradeIntent", "hero", "rank", "level", "point");
        GD.Print($"[NetworkAuthorityTest] hostId={hostId} clientId={clientId} distinct={hostId != clientId} registered={registered} owned={owned} resolved={resolved} rejected={rejected} routerSignatures={routerSignatures} legacyRemoved={legacyRemoved} payloadsSafe={payloadsSafe}");
    }

    private static bool HasIntent(string name, params System.Type[] parameterTypes) =>
        typeof(NetworkIntentRouter).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, parameterTypes) != null;

    private static bool HasNoNetworkManagerMethod(string name) =>
        typeof(NetworkManager).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null;

    private static bool HasNoPayloadParameter(string methodName, params string[] forbiddenTerms)
    {
        MethodInfo method = typeof(NetworkIntentRouter).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) return false;
        foreach (ParameterInfo parameter in method.GetParameters())
        {
            foreach (string forbidden in forbiddenTerms)
            {
                if (parameter.Name.Contains(forbidden, System.StringComparison.OrdinalIgnoreCase)) return false;
            }
        }
        return true;
    }
}
