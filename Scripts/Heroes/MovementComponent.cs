using Godot;

/// <summary>
/// Owns locomotion state for a CharacterBody3D. Input remains outside this
/// component so player, AI, and tests can issue the same movement commands.
/// </summary>
public partial class MovementComponent : Node
{
    [Export]
    public float Speed = 8.0f;

    [Export]
    public float DestinationStopDistance = 0.15f;

    public Vector3 Destination { get; private set; }
    public bool HasDestination { get; private set; }

    private CharacterBody3D _owner;

    public override void _Ready()
    {
        _owner = GetParent() as CharacterBody3D;
        if (_owner == null)
        {
            GD.PushError($"{nameof(MovementComponent)} must be a child of CharacterBody3D.");
        }
    }

    public void SetDestination(Vector3 destination)
    {
        Destination = destination;
        HasDestination = true;
    }

    public void ClearDestination()
    {
        HasDestination = false;
    }

    public Vector3 GetDestinationDirection()
    {
        if (!HasDestination || _owner == null)
        {
            return Vector3.Zero;
        }

        Vector3 offset = Destination - _owner.GlobalPosition;
        offset.Y = 0.0f;

        if (offset.LengthSquared() <= DestinationStopDistance * DestinationStopDistance)
        {
            ClearDestination();
            return Vector3.Zero;
        }

        return offset.Normalized();
    }

    public void MoveInDirection(Vector3 direction, float? speedOverride = null)
    {
        if (_owner == null)
        {
            return;
        }

        direction.Y = 0.0f;
        if (direction.LengthSquared() > 0.0001f)
        {
            direction = direction.Normalized();
            _owner.LookAt(_owner.GlobalPosition + direction, Vector3.Up, true);
        }
        else
        {
            direction = Vector3.Zero;
        }

        float currentSpeed = speedOverride ?? Speed;
        _owner.Velocity = new Vector3(
            direction.X * currentSpeed,
            _owner.Velocity.Y,
            direction.Z * currentSpeed
        );
        _owner.MoveAndSlide();
    }

    public void Dash(Vector3 direction, float distance)
    {
        if (_owner == null || direction.LengthSquared() <= 0.0001f || distance <= 0.0f) return;
        direction.Y = 0.0f;
        direction = direction.Normalized();
        _owner.LookAt(_owner.GlobalPosition + direction, Vector3.Up, true);
        _owner.GlobalPosition += direction * distance;
        ClearDestination();
    }
}
