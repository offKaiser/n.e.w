using Godot;

public partial class NyxaraDashAbility : Ability
{
    [Export] public float DashDistance = 3.5f;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        MovementComponent movement = caster.GetNodeOrNull<MovementComponent>("MovementComponent");
        Node3D targetOwner = target?.GetParent<Node3D>();
        if (movement == null || targetOwner == null) return false;
        Vector3 direction = targetOwner.GlobalPosition - caster.GlobalPosition;
        direction.Y = 0.0f;
        if (direction.LengthSquared() <= 0.001f) return false;
        movement.Dash(direction.Normalized(), DashDistance);
        return true;
    }
}
