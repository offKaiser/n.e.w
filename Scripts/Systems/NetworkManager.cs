using Godot;

public partial class NetworkManager : Node
{
    [Export]
    public int Port = 7000;

    private Label _statusLabel;
    private bool _sessionStarted;
    private long _nextMinionId;

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
            SpawnPlayer(peerId);
            RpcId(peerId, nameof(SpawnPlayer), peerId);
            SynchronizeExistingMinions(peerId);
            SynchronizeExistingState(peerId);
        }

        SetStatus($"Jogador {peerId} conectado.");
    }

    private void OnPeerDisconnected(long peerId)
    {
        RemovePlayer(peerId);
        if (IsServer)
        {
            Rpc(nameof(RemovePlayer), peerId);
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

    public void PublishPlayerTransform(string playerName, Vector3 position, Vector3 rotation)
    {
        if (!SessionActive)
        {
            return;
        }

        Rpc(nameof(ReceivePlayerTransform), playerName, position, rotation);
    }

    public string AllocateMinionName() => $"NetworkMinion_{++_nextMinionId}";

    public void ReplicateMinionSpawn(MinionController minion)
    {
        if (SessionActive && IsServer)
            Rpc(nameof(SpawnNetworkMinion), minion.Name, (int)minion.Team, (int)minion.Type, minion.LaneDirection, minion.GlobalPosition);
    }

    public void PublishMinionTransform(string minionName, Vector3 position, Vector3 rotation)
    {
        if (SessionActive && IsServer) Rpc(nameof(ReceiveMinionTransform), minionName, position, rotation);
    }

    public void PublishEnemyTransform(string enemyName, Vector3 position, Vector3 rotation)
    {
        if (SessionActive && IsServer) Rpc(nameof(ReceiveEnemyTransform), enemyName, position, rotation);
    }

    public void BroadcastTimedVfx(Vector3 position, Color color, float radius, float duration)
    {
        if (SessionActive && IsServer) Rpc(nameof(ReceiveTimedVfx), position, color, radius, duration);
    }

    public void BroadcastBeamVfx(Vector3 from, Vector3 to, Color color, float width, float duration)
    {
        if (SessionActive && IsServer) Rpc(nameof(ReceiveBeamVfx), from, to, color, width, duration);
    }

    public void RequestDamage(NodePath targetPath, float damage, NodePath sourcePath)
    {
        if (!SessionActive)
        {
            return;
        }

        if (IsServer)
        {
            ApplyDamageRequest(targetPath, damage, sourcePath);
        }
        else
        {
            RpcId(1, nameof(ApplyDamageRequest), targetPath, damage, sourcePath);
        }
    }

    public void BroadcastHealth(NodePath targetPath, float health)
    {
        if (SessionActive && IsServer)
        {
            Rpc(nameof(ReceiveHealth), targetPath, health);
        }
    }

    public void RequestManaSpend(NodePath heroPath, float amount)
    {
        if (!SessionActive)
        {
            return;
        }

        if (IsServer)
        {
            ApplyManaSpendRequest(heroPath, amount);
        }
        else
        {
            RpcId(1, nameof(ApplyManaSpendRequest), heroPath, amount);
        }
    }

    public void RequestAbilityCast(HeroController hero, string slot, HealthComponent target)
    {
        if (!SessionActive) return;
        NodePath targetPath = target?.GetParent().GetPath() ?? new NodePath();
        if (IsServer)
            ApplyAbilityCast(hero.GetPath(), slot, targetPath);
        else
            RpcId(1, nameof(ApplyAbilityCast), hero.GetPath(), slot, targetPath);
    }

    public void RequestAbilityUpgrade(HeroController hero, string slot)
    {
        if (!SessionActive) return;
        if (IsServer) ApplyAbilityUpgrade(hero.GetPath(), slot);
        else RpcId(1, nameof(ApplyAbilityUpgrade), hero.GetPath(), slot);
    }

    public void BroadcastMana(NodePath heroPath, float mana)
    {
        if (SessionActive && IsServer)
        {
            Rpc(nameof(ReceiveMana), heroPath, mana);
        }
    }

    public void BroadcastProgression(NodePath componentPath, int level, int experience, int skillPoints)
    {
        if (SessionActive && IsServer)
            Rpc(nameof(ReceiveProgression), componentPath, level, experience, skillPoints);
    }

    public void BroadcastGold(NodePath componentPath, int gold)
    {
        if (SessionActive && IsServer)
            Rpc(nameof(ReceiveGold), componentPath, gold);
    }

    public void BroadcastAbyssEnergy(NodePath componentPath, float energy)
    {
        if (SessionActive && IsServer)
            Rpc(nameof(ReceiveAbyssEnergy), componentPath, energy);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ApplyManaSpendRequest(NodePath heroPath, float amount)
    {
        if (!IsServer)
        {
            return;
        }

        ManaComponent mana = GetTree().Root.GetNodeOrNull<Node>(heroPath)?.GetNodeOrNull<ManaComponent>("ManaComponent");
        if (mana != null && mana.SpendMana(amount))
        {
            BroadcastMana(heroPath, mana.CurrentMana);
        }
    }

    [Rpc]
    private void ReceiveMana(NodePath heroPath, float mana)
    {
        GetTree().Root.GetNodeOrNull<Node>(heroPath)?.GetNodeOrNull<ManaComponent>("ManaComponent")?.SynchronizeMana(mana);
    }

    [Rpc]
    private void ReceiveProgression(NodePath componentPath, int level, int experience, int skillPoints)
    {
        GetTree().Root.GetNodeOrNull<ProgressionComponent>(componentPath)?.SynchronizeProgression(level, experience, skillPoints);
    }

    [Rpc]
    private void ReceiveGold(NodePath componentPath, int gold)
    {
        GetTree().Root.GetNodeOrNull<GoldComponent>(componentPath)?.SynchronizeGold(gold);
    }

    [Rpc]
    private void ReceiveAbyssEnergy(NodePath componentPath, float energy)
    {
        GetTree().Root.GetNodeOrNull<AbyssEnergyComponent>(componentPath)?.SynchronizeEnergy(energy);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ApplyDamageRequest(NodePath targetPath, float damage, NodePath sourcePath)
    {
        if (!IsServer)
        {
            return;
        }

        Node target = GetTree().Root.GetNodeOrNull<Node>(targetPath);
        Node source = sourcePath.IsEmpty ? null : GetTree().Root.GetNodeOrNull<Node>(sourcePath);
        target?.GetNodeOrNull<HealthComponent>("HealthComponent")?.TakeDamage(damage, source);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ApplyAbilityCast(NodePath heroPath, string slot, NodePath targetPath)
    {
        if (!IsServer) return;
        HeroController hero = GetTree().Root.GetNodeOrNull<HeroController>(heroPath);
        if (hero == null) return;
        long sender = Multiplayer.GetRemoteSenderId();
        if (sender != 0 && sender != 1 && hero.GetMultiplayerAuthority() != sender) return;
        HealthComponent target = targetPath.IsEmpty ? null : GetTree().Root.GetNodeOrNull<Node>(targetPath)?.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (!hero.TryCastAbilityLocal(slot, target)) return;
        Ability ability = hero.GetAbility(slot);
        Rpc(nameof(ReceiveAbilityCooldown), hero.Name, slot, ability.RemainingCooldown);
        Rpc(nameof(ReceiveAbilityTransform), hero.Name, hero.GlobalPosition, hero.Rotation);
        Vector3 effectPosition = target?.GetParent<Node3D>().GlobalPosition ?? hero.GlobalPosition;
        Rpc(nameof(ReceiveAbilityVisual), hero.Name, slot, effectPosition);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ApplyAbilityUpgrade(NodePath heroPath, string slot)
    {
        if (!IsServer) return;
        HeroController hero = GetTree().Root.GetNodeOrNull<HeroController>(heroPath);
        if (hero == null) return;
        long sender = Multiplayer.GetRemoteSenderId();
        if (sender != 0 && sender != 1 && hero.GetMultiplayerAuthority() != sender) return;
        if (!hero.TryIncreaseAbilityLocal(slot)) return;
        Rpc(nameof(ReceiveAbilityRank), hero.Name, slot, hero.GetAbility(slot).Rank);
    }

    [Rpc]
    private void ReceiveAbilityCooldown(string heroName, string slot, float remaining)
    {
        GetTree().CurrentScene?.GetNodeOrNull<HeroController>(heroName)?.GetAbility(slot)?.SynchronizeCooldown(remaining);
    }

    [Rpc]
    private void ReceiveAbilityTransform(string heroName, Vector3 position, Vector3 rotation)
    {
        GetTree().CurrentScene?.GetNodeOrNull<HeroController>(heroName)?.ApplyNetworkTransform(position, rotation);
    }

    [Rpc]
    private void ReceiveAbilityRank(string heroName, string slot, int rank)
    {
        GetTree().CurrentScene?.GetNodeOrNull<HeroController>(heroName)?.GetAbility(slot)?.SynchronizeRank(rank);
    }

    [Rpc]
    private void ReceiveAbilityVisual(string heroName, string slot, Vector3 effectPosition)
    {
        Node3D scene = GetTree().CurrentScene as Node3D;
        HeroController hero = scene?.GetNodeOrNull<HeroController>(heroName);
        if (scene == null || hero == null) return;
        Color color = new Color(0.42f, 0.05f, 0.95f);
        if (slot == "W")
        {
            BeamVfx beam = new BeamVfx();
            scene.AddChild(beam);
            beam.Configure(hero.GlobalPosition + Vector3.Up, effectPosition + Vector3.Up, color, 0.09f, 0.38f);
            return;
        }
        TimedVfx vfx = new TimedVfx();
        scene.AddChild(vfx);
        vfx.GlobalPosition = effectPosition + Vector3.Up * 0.35f;
        float radius = slot == "R" ? 5.0f : slot == "Q" ? 3.0f : 1.8f;
        float duration = slot == "R" ? 5.0f : 0.65f;
        vfx.Configure(color, radius, duration);
    }

    [Rpc]
    private void ReceiveHealth(NodePath targetPath, float health)
    {
        Node target = GetTree().Root.GetNodeOrNull<Node>(targetPath);
        target?.GetNodeOrNull<HealthComponent>("HealthComponent")?.SynchronizeHealth(health);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ReceivePlayerTransform(string playerName, Vector3 position, Vector3 rotation)
    {
        HeroController hero = GetTree().CurrentScene?.GetNodeOrNull<HeroController>(playerName);
        long sender = Multiplayer.GetRemoteSenderId();
        if (hero != null && sender != 0 && hero.GetMultiplayerAuthority() != sender)
        {
            return;
        }
        if (hero != null && !hero.IsMultiplayerAuthority())
        {
            hero.ApplyNetworkTransform(position, rotation);
        }
    }

    [Rpc]
    private void SpawnNetworkMinion(string minionName, int team, int type, Vector3 laneDirection, Vector3 position)
    {
        Node scene = GetTree().CurrentScene;
        if (scene == null || scene.GetNodeOrNull<Node>(minionName) != null) return;
        PackedScene minionScene = GD.Load<PackedScene>("res://Scenes/Units/Minion.tscn");
        MinionController minion = minionScene.Instantiate<MinionController>();
        minion.Name = minionName;
        minion.Configure((MinionTeam)team, laneDirection, (MinionType)type);
        scene.AddChild(minion);
        minion.GlobalPosition = position;
    }

    [Rpc]
    private void ReceiveMinionTransform(string minionName, Vector3 position, Vector3 rotation)
    {
        GetTree().CurrentScene?.GetNodeOrNull<MinionController>(minionName)?.ApplyNetworkTransform(position, rotation);
    }

    [Rpc]
    private void ReceiveEnemyTransform(string enemyName, Vector3 position, Vector3 rotation)
    {
        GetTree().CurrentScene?.GetNodeOrNull<EnemyController>(enemyName)?.ApplyNetworkTransform(position, rotation);
    }

    [Rpc]
    private void ReceiveTimedVfx(Vector3 position, Color color, float radius, float duration)
    {
        Node3D scene = GetTree().CurrentScene as Node3D;
        if (scene == null) return;
        TimedVfx vfx = new TimedVfx();
        scene.AddChild(vfx);
        vfx.GlobalPosition = position;
        vfx.Configure(color, radius, duration);
    }

    [Rpc]
    private void ReceiveBeamVfx(Vector3 from, Vector3 to, Color color, float width, float duration)
    {
        Node3D scene = GetTree().CurrentScene as Node3D;
        if (scene == null) return;
        BeamVfx beam = new BeamVfx();
        scene.AddChild(beam);
        beam.Configure(from, to, color, width, duration);
    }

    private void SynchronizeExistingMinions(long peerId)
    {
        foreach (Node node in GetTree().GetNodesInGroup("minions"))
        {
            if (node is MinionController minion)
                RpcId(peerId, nameof(SpawnNetworkMinion), minion.Name, (int)minion.Team, (int)minion.Type, minion.LaneDirection, minion.GlobalPosition);
        }
    }

    private void SynchronizeExistingState(long peerId)
    {
        foreach (Node node in GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D unit) continue;
            HealthComponent health = unit.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health != null) RpcId(peerId, nameof(ReceiveHealth), unit.GetPath(), health.CurrentHealth);
        }
        foreach (Node node in GetTree().GetNodesInGroup("heroes"))
        {
            if (node is not HeroController hero) continue;
            ManaComponent mana = hero.GetNodeOrNull<ManaComponent>("ManaComponent");
            ProgressionComponent progression = hero.GetNodeOrNull<ProgressionComponent>("ProgressionComponent");
            GoldComponent gold = hero.GetNodeOrNull<GoldComponent>("GoldComponent");
            AbyssEnergyComponent abyss = hero.GetNodeOrNull<AbyssEnergyComponent>("AbyssEnergyComponent");
            if (mana != null) RpcId(peerId, nameof(ReceiveMana), hero.GetPath(), mana.CurrentMana);
            if (progression != null) RpcId(peerId, nameof(ReceiveProgression), progression.GetPath(), progression.Level, progression.Experience, progression.SkillPoints);
            if (gold != null) RpcId(peerId, nameof(ReceiveGold), gold.GetPath(), gold.Gold);
            if (abyss != null) RpcId(peerId, nameof(ReceiveAbyssEnergy), abyss.GetPath(), abyss.Energy);
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

    [Rpc]
    private void SpawnPlayer(long peerId)
    {
        string playerName = $"Hero_{peerId}";
        if (GetTree().CurrentScene?.GetNodeOrNull<Node>(playerName) != null)
        {
            return;
        }

        HeroController template = GetLocalHero();
        if (template == null)
        {
            return;
        }

        HeroController player = template.Duplicate() as HeroController;
        player.Name = playerName;
        player.SetMultiplayerAuthority((int)peerId);
        GetTree().CurrentScene.AddChild(player);
        player.GlobalPosition = new Vector3(-2.0f, 0.0f, 3.0f);

        if (player.IsMultiplayerAuthority())
        {
            GetTree().CurrentScene.GetNodeOrNull<CameraFollow>("Camera3D")?.SetTarget(player);
        }
    }

    [Rpc]
    private void RemovePlayer(long peerId)
    {
        GetTree().CurrentScene?.GetNodeOrNull<Node>($"Hero_{peerId}")?.QueueFree();
    }

    private HeroController GetLocalHero()
    {
        return GetTree().CurrentScene?.GetNodeOrNull<HeroController>("Hero");
    }
}
