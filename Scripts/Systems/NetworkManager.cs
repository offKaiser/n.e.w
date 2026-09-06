using Godot;

public partial class NetworkManager : Node
{
    [Export]
    public int Port = 7000;

    private Label _statusLabel;
    private bool _sessionStarted;
    private long _nextMinionId;
    public NetworkEntityRegistry EntityRegistry { get; } = new();
    public NetworkPeerOwnership PeerOwnership { get; } = new();
    public NetworkIntentRouter IntentRouter { get; private set; }
    public NetworkStateReplicator StateReplicator { get; private set; }
    public NetworkPresentationReplicator PresentationReplicator { get; private set; }

    public bool SessionActive => _sessionStarted && IsNetworkActive;
    public bool IsServer => IsNetworkActive && Multiplayer.IsServer();

    private bool IsNetworkActive
    {
        get
        {
            MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
            return peer != null && peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
        }
    }

    public override void _Ready()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;

        CreateStatusLabel();
        IntentRouter = new NetworkIntentRouter { Name = "NetworkIntentRouter" };
        AddChild(IntentRouter);
        StateReplicator = new NetworkStateReplicator { Name = "NetworkStateReplicator" };
        AddChild(StateReplicator);
        PresentationReplicator = new NetworkPresentationReplicator { Name = "NetworkPresentationReplicator" };
        AddChild(PresentationReplicator);
        GetLocalHero()?.SetMultiplayerAuthority(1);
        TryJoinFromCommandLine();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo || SessionActive)
        {
            return;
        }

        if (keyEvent.Keycode == Key.F1)
        {
            HostGame();
        }
        else if (keyEvent.Keycode == Key.F2)
        {
            JoinGame("127.0.0.1");
        }
    }

    public void HostGame()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error result = peer.CreateServer(Port);
        if (result != Error.Ok)
        {
            SetStatus($"Falha ao criar servidor: {result}");
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        _sessionStarted = true;
        RegisterHostPlayer();
        RegisterFixedAuthorityEntities();
        SetStatus($"SERVIDOR LAN ativo na porta {Port}\nAguardando jogadores...");
    }

    public void JoinGame(string address)
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error result = peer.CreateClient(address, Port);
        if (result != Error.Ok)
        {
            SetStatus($"Falha ao conectar: {result}");
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
        _sessionStarted = true;
        SetStatus($"Conectando a {address}:{Port}...");
    }

    private void TryJoinFromCommandLine()
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument == "--host")
            {
                HostGame();
                return;
            }
            if (argument.StartsWith("--join="))
            {
                JoinGame(argument[7..]);
                return;
            }
        }

        SetStatus("F1: hospedar partida LAN | F2: entrar em localhost\nOutro PC: execute com -- --join=IP_DO_HOST");
    }

    private void OnPeerConnected(long peerId)
    {
        if (IsServer)
        {
            int entityId = CreateAuthoritativePlayer(peerId);
            if (entityId > 0) SynchronizeExistingPlayers(peerId);
            SynchronizeSceneIdentityReplicas(peerId);
            StateReplicator.SendPlayerSnapshots(peerId);
            StateReplicator.SendWorldSnapshots(peerId);
        }

        SetStatus($"Jogador {peerId} conectado.");
    }

    private void OnPeerDisconnected(long peerId)
    {
        int entityId = 0;
        PeerOwnership.TryGetControlledEntityId(peerId, out entityId);
        PeerOwnership.Remove(peerId);
        StateReplicator?.ForgetAuthoritativePlayer(entityId);
        RemovePlayer(peerId, entityId);
        if (IsServer)
        {
            Rpc(nameof(RemovePlayer), peerId, entityId);
        }
        SetStatus($"Jogador {peerId} desconectou.");
    }

    private void OnConnectedToServer()
    {
        ClearNetworkMinions();
        SetStatus($"Conectado ao servidor. Meu ID: {Multiplayer.GetUniqueId()}");
    }

    private void OnConnectionFailed()
    {
        _sessionStarted = false;
        SetStatus("Nao foi possivel conectar ao servidor.");
    }

    private void OnServerDisconnected()
    {
        _sessionStarted = false;
        SetStatus("Conexao com o servidor encerrada.");
    }

    public override void _ExitTree()
    {
        // Prevent gameplay nodes from issuing RPCs while Godot tears down the peer.
        _sessionStarted = false;
        EntityRegistry.Clear();
        PeerOwnership.Clear();
    }

    private void CreateStatusLabel()
    {
        CanvasLayer layer = new CanvasLayer();
        AddChild(layer);

        _statusLabel = new Label();
        _statusLabel.Position = new Vector2(24.0f, 100.0f);
        _statusLabel.AddThemeFontSizeOverride("font_size", 18);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.9f, 1.0f));
        layer.AddChild(_statusLabel);
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
        GD.Print(message);
    }

    public string AllocateMinionName() => $"NetworkMinion_{++_nextMinionId}";

    public int GetEntityId(Node node) => node?.GetNodeOrNull<NetworkIdentity>("NetworkIdentity")?.NetworkEntityId ?? 0;

    public bool IsControlledEntity(HeroController hero)
    {
        int entityId = GetEntityId(hero);
        return entityId > 0 && PeerOwnership.TryGetPeerForEntity(entityId, out _);
    }

    public bool TryResolveControlledEntity(long peerId, out HeroController hero)
    {
        hero = null;
        return PeerOwnership.TryGetControlledEntityId(peerId, out int entityId) &&
            EntityRegistry.TryResolve(entityId, out Node node) && (hero = node as HeroController) != null;
    }

    private void SynchronizeExistingPlayers(long peerId)
    {
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not HeroController hero || hero.Name == "Hero") continue;
            int entityId = GetEntityId(hero);
            if (entityId > 0)
                RpcId(peerId, nameof(SpawnPlayer), (long)hero.GetMultiplayerAuthority(), entityId);
        }
    }

    private void ClearNetworkMinions()
    {
        foreach (Node node in GetTree().GetNodesInGroup("minions"))
        {
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }
    }

    private int CreateAuthoritativePlayer(long peerId)
    {
        string playerName = $"Hero_{peerId}";
        if (GetTree().CurrentScene?.GetNodeOrNull<HeroController>(playerName) is HeroController existing)
        {
            return GetEntityId(existing);
        }

        HeroController template = GetLocalHero();
        if (template == null)
        {
            return 0;
        }

        HeroController player = template.Duplicate() as HeroController;
        player.Name = playerName;
        player.SetMultiplayerAuthority((int)peerId);
        GetTree().CurrentScene.AddChild(player);
        player.GlobalPosition = new Vector3(-2.0f, 0.0f, 3.0f);
        int entityId = RegisterAuthoritativePlayer(player, peerId);
        if (entityId <= 0)
        {
            player.QueueFree();
            return 0;
        }

        return entityId;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void SpawnPlayer(long peerId, int entityId)
    {
        string playerName = $"Hero_{peerId}";
        if (GetTree().CurrentScene?.GetNodeOrNull<Node>(playerName) != null) return;

        HeroController template = GetLocalHero();
        if (template == null || entityId <= 0) return;

        HeroController player = template.Duplicate() as HeroController;
        player.Name = playerName;
        player.SetMultiplayerAuthority((int)peerId);
        GetTree().CurrentScene.AddChild(player);
        player.GlobalPosition = new Vector3(-2.0f, 0.0f, 3.0f);
        RegisterReplicaEntity(player, entityId);
        if (player.IsMultiplayerAuthority())
            GetTree().CurrentScene.GetNodeOrNull<CameraFollow>("Camera3D")?.SetTarget(player);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void RemovePlayer(long peerId, int entityId)
    {
        if (entityId > 0) EntityRegistry.Unregister(entityId);
        GetTree().CurrentScene?.GetNodeOrNull<Node>($"Hero_{peerId}")?.QueueFree();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void RegisterSceneEntityReplica(string sceneNodeName, int entityId)
    {
        if (entityId <= 0) return;
        Node node = GetTree().CurrentScene?.GetNodeOrNull<Node>(sceneNodeName);
        if (node != null)
        {
            if (node is EnemyController enemy) enemy.RemoteRepresentation = true;
            if (node is TowerController tower) tower.RemoteRepresentation = true;
            RegisterReplicaEntity(node, entityId);
        }
    }

    private HeroController GetLocalHero()
    {
        return GetTree().CurrentScene?.GetNodeOrNull<HeroController>("Hero");
    }

    private void RegisterHostPlayer()
    {
        HeroController hero = GetLocalHero();
        if (hero != null) RegisterAuthoritativePlayer(hero, 1);
    }

    private int RegisterAuthoritativePlayer(HeroController player, long peerId)
    {
        int entityId = RegisterAuthorityEntity(player);
        if (entityId <= 0) return 0;
        if (!PeerOwnership.HasPeer(peerId) && !PeerOwnership.Assign(peerId, entityId))
        {
            EntityRegistry.Unregister(entityId);
            return 0;
        }
        StateReplicator?.ObserveAuthoritativePlayer(player, entityId);
        return entityId;
    }

    private int RegisterAuthorityEntity(Node node)
    {
        NetworkIdentity identity = node.GetNodeOrNull<NetworkIdentity>("NetworkIdentity");
        if (identity != null && identity.IsValid && EntityRegistry.Contains(identity.NetworkEntityId))
            return identity.NetworkEntityId;

        if (identity != null && identity.IsValid)
        {
            node.RemoveChild(identity);
            identity.QueueFree();
            identity = null;
        }
        identity ??= new NetworkIdentity { Name = "NetworkIdentity" };
        if (identity.GetParent() == null) node.AddChild(identity);
        int entityId = EntityRegistry.AllocateId();
        return identity.Assign(entityId) && EntityRegistry.Register(entityId, node) ? entityId : 0;
    }

    public void RegisterAuthoritativeMinion(MinionController minion)
    {
        int entityId = RegisterAuthorityEntity(minion);
        if (entityId > 0) { StateReplicator.ObserveAuthoritativeWorld(minion, entityId); StateReplicator.PublishMinionSpawn(minion, entityId); }
    }

    public void RegisterReplicaWorldEntity(Node3D entity, int entityId) => RegisterReplicaEntity(entity, entityId);

    private void RegisterReplicaEntity(Node node, int entityId)
    {
        NetworkIdentity identity = node.GetNodeOrNull<NetworkIdentity>("NetworkIdentity");
        if (identity != null && identity.IsValid && identity.NetworkEntityId != entityId)
        {
            node.RemoveChild(identity);
            identity.QueueFree();
            identity = null;
        }
        identity ??= new NetworkIdentity { Name = "NetworkIdentity" };
        if (identity.GetParent() == null) node.AddChild(identity);
        if (identity.Assign(entityId)) EntityRegistry.Register(entityId, node);
    }

    private void RegisterFixedAuthorityEntities()
    {
        if (GetTree().CurrentScene == null) return;
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is EnemyController || node is TowerController || node is MinionController)
            {
                int entityId = RegisterAuthorityEntity(node);
                if (entityId > 0) StateReplicator.ObserveAuthoritativeWorld((Node3D)node, entityId);
            }
        }
    }

    private void SynchronizeSceneIdentityReplicas(long peerId)
    {
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not HeroController && node is not EnemyController && node is not TowerController) continue;
            int entityId = GetEntityId(node);
            if (entityId > 0) RpcId(peerId, nameof(RegisterSceneEntityReplica), node.Name, entityId);
        }
    }
}
