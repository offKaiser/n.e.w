using Godot;

public partial class HeroController : CharacterBody3D, IStatusEffectReceiver, ITeamMember
{
    [Export]
    public MinionTeam Team = MinionTeam.Blue;

    TeamId ITeamMember.TeamId => Team == MinionTeam.Red ? TeamId.Red : TeamId.Blue;

    [Export]
    public float Speed = 8.0f;

    [Export]
    public float DestinationStopDistance = 0.15f;

    [Export]
    public float ClickRayLength = 500.0f;

    private MeshInstance3D _destinationMarker;
    private MovementComponent _movement;
    private TargetingComponent _targeting;
    private CombatComponent _combat;
    private AbilityController _abilityController;
    private float _speedMultiplier = 1.0f;
    private double _speedBoostEndTime;

    public override void _Ready()
    {
        AddToGroup("combat_units");
        _destinationMarker = GetNodeOrNull<MeshInstance3D>("../DestinationMarker");
        _movement = GetNodeOrNull<MovementComponent>("MovementComponent");
        _targeting = GetNodeOrNull<TargetingComponent>("TargetingComponent");
        EnsureCoreComponents();
        SynchronizeMovementSettings();
        _combat = GetNodeOrNull<CombatComponent>("CombatComponent");
        _abilityController = GetNodeOrNull<AbilityController>("AbilityController");
        EnsureAbilityController();
        EnsureRewardComponent();
        if (_destinationMarker != null)
        {
            _destinationMarker.Visible = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Key1) TryIncreaseAbility("Q");
            if (keyEvent.Keycode == Key.Key2) TryIncreaseAbility("W");
            if (keyEvent.Keycode == Key.Key3) TryIncreaseAbility("E");
            if (keyEvent.Keycode == Key.Key4) TryIncreaseAbility("R");
            return;
        }

        if (@event is not InputEventMouseButton mouseButton ||
            mouseButton.ButtonIndex != MouseButton.Left ||
            !mouseButton.Pressed)
        {
            return;
        }

        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null)
        {
            return;
        }

        Vector2 mousePosition = GetViewport().GetMousePosition();
        Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
        Vector3 rayEnd = rayOrigin + camera.ProjectRayNormal(mousePosition) * ClickRayLength;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        Godot.Collections.Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0)
        {
            return;
        }

        Node hitNode = hit["collider"].AsGodotObject() as Node;
        HealthComponent target = hitNode?.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (target != null && target.IsAlive)
        {
            _movement.ClearDestination();
            HideDestinationMarker();
            NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
            if (network != null && network.SessionActive)
            {
                int targetEntityId = network.GetEntityId(target.GetParent());
                if (targetEntityId > 0)
                {
                    network.IntentRouter.RequestBasicAttack(targetEntityId);
                    return;
                }
            }
            _targeting.SetTarget(target);
            _combat?.SetTarget(target);
            return;
        }

        _targeting.ClearTarget();
        _combat?.ClearTarget();
        Vector3 destination = (Vector3)hit["position"];
        NetworkManager movementNetwork = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (movementNetwork != null && movementNetwork.SessionActive)
        {
            movementNetwork.IntentRouter.RequestMove(destination);
            return;
        }
        _movement.SetDestination(destination);

        if (_destinationMarker != null)
        {
            _destinationMarker.GlobalPosition = destination + Vector3.Up * 0.06f;
            _destinationMarker.Visible = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        bool readsLocalInput = IsMultiplayerAuthority();
        bool simulatesRemoteAuthority = network != null && network.IsServer && network.IsControlledEntity(this);
        if (!readsLocalInput && !simulatesRemoteAuthority)
        {
            return;
        }

        if (readsLocalInput && Input.IsActionJustPressed("ability_q"))
        {
            TryCastAbility("Q");
        }

        if (Input.IsActionJustPressed("ability_w"))
        {
            TryCastAbility("W");
        }

        if (Input.IsActionJustPressed("ability_e"))
        {
            TryCastAbility("E");
        }

        if (Input.IsActionJustPressed("ability_r"))
        {
            TryCastAbility("R");
        }

        Vector2 inputDirection = readsLocalInput ? Input.GetVector("move_left", "move_right", "move_forward", "move_backward") : Vector2.Zero;
        Vector3 direction;
        SynchronizeMovementSettings();

        if (inputDirection != Vector2.Zero)
        {
            _movement.ClearDestination();
            HideDestinationMarker();
            _targeting.ClearTarget();
            _combat?.ClearTarget();
            direction = new Vector3(inputDirection.X, 0.0f, inputDirection.Y);
        }
        else if (_targeting.HasValidTarget)
        {
            direction = GetAttackTargetDirection(_targeting.CurrentTarget);
        }
        else
        {
            _combat?.ClearTarget();
            direction = _movement.GetDestinationDirection();
            if (!_movement.HasDestination)
            {
                HideDestinationMarker();
            }
        }

        _movement.MoveInDirection(direction, GetCurrentSpeed());
    }

    public void ApplyNetworkTransform(Vector3 position, Vector3 rotation)
    {
        GlobalPosition = position;
        Rotation = rotation;
    }

    public Ability GetAbility(string slot) => _abilityController?.GetAbility(slot);

    public bool TryCastAbilityLocal(string slot, HealthComponent target)
    {
        return _abilityController?.TryCast(slot, target) ?? false;
    }

    public void ReduceAbilityCooldowns(float seconds)
    {
        _abilityController?.GetAbility("Q")?.ReduceCooldown(seconds);
        _abilityController?.GetAbility("W")?.ReduceCooldown(seconds);
        _abilityController?.GetAbility("E")?.ReduceCooldown(seconds);
        _abilityController?.GetAbility("R")?.ReduceCooldown(seconds);
    }

    private void TryCastAbility(string slot)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive)
        {
            HealthComponent target = GetCurrentTarget();
            int targetEntityId = target == null ? 0 : network.GetEntityId(target.GetParent());
            network.IntentRouter.RequestAbilityCast(slot, targetEntityId, GlobalPosition, target != null, true);
            return;
        }
        TryCastAbilityLocal(slot, GetCurrentTarget());
    }

    public bool TryIncreaseAbilityLocal(string slot)
    {
        return _abilityController?.TryIncreaseRank(slot, GetNodeOrNull<ProgressionComponent>("ProgressionComponent")) ?? false;
    }

    private void TryIncreaseAbility(string slot)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive)
        {
            network.IntentRouter.RequestAbilityUpgrade(slot);
            return;
        }
        TryIncreaseAbilityLocal(slot);
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        _speedMultiplier = Mathf.Max(_speedMultiplier, multiplier);
        _speedBoostEndTime = Time.GetTicksMsec() / 1000.0 + duration;
    }

    public void ApplyMoveIntent(Vector3 destination)
    {
        _targeting?.ClearTarget();
        _combat?.ClearTarget();
        _movement?.SetDestination(destination);
        HideDestinationMarker();
    }

    public void ApplyBasicAttackIntent(HealthComponent target)
    {
        _movement?.ClearDestination();
        HideDestinationMarker();
        _targeting?.SetTarget(target);
        _combat?.SetTarget(target);
    }

    public void ApplySlow(float multiplier, float duration)
    {
        _speedMultiplier = Mathf.Min(_speedMultiplier, multiplier);
        _speedBoostEndTime = Time.GetTicksMsec() / 1000.0 + duration;
    }

    private float GetCurrentSpeed()
    {
        if (Time.GetTicksMsec() / 1000.0 >= _speedBoostEndTime)
        {
            _speedMultiplier = 1.0f;
        }

        return Speed * _speedMultiplier;
    }

    private Vector3 GetAttackTargetDirection(HealthComponent target)
    {
        if (target == null || _combat == null)
        {
            return Vector3.Zero;
        }

        _combat.SetTarget(target);
        if (_combat.IsTargetInRange(GlobalPosition))
        {
            _combat.TryAttack(GlobalPosition);
            return Vector3.Zero;
        }

        return _combat.GetApproachDirection(GlobalPosition);
    }

    private HealthComponent GetCurrentTarget()
    {
        return _targeting?.CurrentTarget ?? _combat?.CurrentTarget;
    }

    private void HideDestinationMarker()
    {
        if (_destinationMarker != null)
        {
            _destinationMarker.Visible = false;
        }
    }

    private void EnsureCoreComponents()
    {
        // Main.tscn remains untouched during this migration. These runtime
        // fallbacks keep its existing serialized Hero node operational.
        if (_movement == null)
        {
            _movement = new MovementComponent { Name = "MovementComponent" };
            AddChild(_movement);
        }

        if (_targeting == null)
        {
            _targeting = new TargetingComponent { Name = "TargetingComponent" };
            AddChild(_targeting);
        }
    }

    private void SynchronizeMovementSettings()
    {
        if (_movement == null)
        {
            return;
        }

        // Keep the existing HeroController exports as a compatibility bridge
        // until Main.tscn is migrated in a later approved phase.
        _movement.Speed = Speed;
        _movement.DestinationStopDistance = DestinationStopDistance;
    }

    private void EnsureAbilityController()
    {
        if (_abilityController == null)
        {
            _abilityController = new AbilityController { Name = "AbilityController" };
            AddChild(_abilityController);
        }
    }

    private void EnsureRewardComponent()
    {
        if (GetNodeOrNull<RewardComponent>("RewardComponent") == null)
            AddChild(new RewardComponent { Name = "RewardComponent", GoldReward = 300, ExperienceReward = 350 });
    }
}
