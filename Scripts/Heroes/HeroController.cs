using Godot;

public partial class HeroController : CharacterBody3D
{
    [Export]
    public MinionTeam Team = MinionTeam.Blue;

    [Export]
    public float Speed = 8.0f;

    [Export]
    public float DestinationStopDistance = 0.15f;

    [Export]
    public float ClickRayLength = 500.0f;

    private Vector3 _destination;
    private bool _hasDestination;
    private MeshInstance3D _destinationMarker;
    private CombatComponent _combat;
    private Ability _abilityQ;
    private Ability _abilityW;
    private Ability _abilityE;
    private Ability _abilityR;
    private float _speedMultiplier = 1.0f;
    private double _speedBoostEndTime;
    private double _nextNetworkSyncTime;

    public override void _Ready()
    {
        AddToGroup("combat_units");
        _destinationMarker = GetNodeOrNull<MeshInstance3D>("../DestinationMarker");
        _combat = GetNodeOrNull<CombatComponent>("CombatComponent");
        _abilityQ = GetNodeOrNull<Ability>("AbilityQ");
        _abilityW = GetNodeOrNull<Ability>("AbilityW");
        _abilityE = GetNodeOrNull<Ability>("AbilityE");
        _abilityR = GetNodeOrNull<Ability>("AbilityR");
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
        if (target != null && target.IsAlive && _combat != null)
        {
            _hasDestination = false;
            HideDestinationMarker();
            _combat.SetTarget(target);
            return;
        }

        _combat?.ClearTarget();
        _destination = (Vector3)hit["position"];
        _hasDestination = true;

        if (_destinationMarker != null)
        {
            _destinationMarker.GlobalPosition = _destination + Vector3.Up * 0.06f;
            _destinationMarker.Visible = true;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }

        if (Input.IsActionJustPressed("ability_q"))
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

        Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        Vector3 direction;

        if (inputDirection != Vector2.Zero)
        {
            _hasDestination = false;
            HideDestinationMarker();
            _combat?.ClearTarget();
            direction = new Vector3(inputDirection.X, 0.0f, inputDirection.Y);
        }
        else if (_combat != null && _combat.HasValidTarget)
        {
            direction = GetAttackTargetDirection();
        }
        else
        {
            direction = GetDestinationDirection();
        }

        Velocity = new Vector3(
            direction.X * GetCurrentSpeed(),
            Velocity.Y,
            direction.Z * GetCurrentSpeed()
        );

        MoveAndSlide();
        PublishNetworkTransform();
    }

    public void ApplyNetworkTransform(Vector3 position, Vector3 rotation)
    {
        GlobalPosition = position;
        Rotation = rotation;
    }

    public Ability GetAbility(string slot) => slot switch
    {
        "Q" => _abilityQ,
        "W" => _abilityW,
        "E" => _abilityE,
        "R" => _abilityR,
        _ => null
    };

    public bool TryCastAbilityLocal(string slot, HealthComponent target)
    {
        return GetAbility(slot)?.TryCast(this, target) ?? false;
    }

    public void ReduceAbilityCooldowns(float seconds)
    {
        _abilityQ?.ReduceCooldown(seconds);
        _abilityW?.ReduceCooldown(seconds);
        _abilityE?.ReduceCooldown(seconds);
        _abilityR?.ReduceCooldown(seconds);
    }

    private void TryCastAbility(string slot)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive && !network.IsServer)
        {
            network.RequestAbilityCast(this, slot, _combat?.CurrentTarget);
            return;
        }
        TryCastAbilityLocal(slot, _combat?.CurrentTarget);
    }

    public bool TryIncreaseAbilityLocal(string slot)
    {
        return GetAbility(slot)?.TryIncreaseRank(GetNodeOrNull<ProgressionComponent>("ProgressionComponent")) ?? false;
    }

    private void TryIncreaseAbility(string slot)
    {
        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        if (network != null && network.SessionActive && !network.IsServer)
        {
            network.RequestAbilityUpgrade(this, slot);
            return;
        }
        TryIncreaseAbilityLocal(slot);
    }

    private void PublishNetworkTransform()
    {
        if (Time.GetTicksMsec() / 1000.0 < _nextNetworkSyncTime)
        {
            return;
        }

        NetworkManager network = GetNodeOrNull<NetworkManager>("/root/NetworkManager");
        network?.PublishPlayerTransform(Name, GlobalPosition, Rotation);
        _nextNetworkSyncTime = Time.GetTicksMsec() / 1000.0 + 0.05;
    }

    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        _speedMultiplier = Mathf.Max(_speedMultiplier, multiplier);
        _speedBoostEndTime = Time.GetTicksMsec() / 1000.0 + duration;
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

    private Vector3 GetAttackTargetDirection()
    {
        Vector3 offset = _combat.TargetPosition - GlobalPosition;
        offset.Y = 0.0f;

        if (offset.LengthSquared() <= _combat.AttackRange * _combat.AttackRange)
        {
            _combat.TryAttack(GlobalPosition);
            return Vector3.Zero;
        }

        Vector3 direction = offset.Normalized();
        LookAt(GlobalPosition + direction, Vector3.Up, true);
        return direction;
    }

    private Vector3 GetDestinationDirection()
    {
        if (!_hasDestination)
        {
            return Vector3.Zero;
        }

        Vector3 offset = _destination - GlobalPosition;
        offset.Y = 0.0f;

        if (offset.LengthSquared() <= DestinationStopDistance * DestinationStopDistance)
        {
            _hasDestination = false;
            HideDestinationMarker();
            return Vector3.Zero;
        }

        Vector3 direction = offset.Normalized();
        LookAt(GlobalPosition + direction, Vector3.Up, true);
        return direction;
    }

    private void HideDestinationMarker()
    {
        if (_destinationMarker != null)
        {
            _destinationMarker.Visible = false;
        }
    }
}
