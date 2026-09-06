using Godot;

public partial class NyxaraCannonAbility : Ability
{
    [Export] public float Damage = 150.0f;
    [Export] public float HitWidth = 0.8f;

    protected override bool Execute(Node3D caster, HealthComponent target)
    {
        Node3D targetOwner = target?.GetParent<Node3D>();
        if (targetOwner == null) return false;
        Vector3 start = caster.GlobalPosition;
        Vector3 end = targetOwner.GlobalPosition;
        foreach (Node node in caster.GetTree().GetNodesInGroup("combat_units"))
        {
            if (node is not Node3D unit || !CombatTeams.IsEnemy(caster, unit)) continue;
            HealthComponent health = unit.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health != null && health.IsAlive && DistanceToSegment(unit.GlobalPosition, start, end) <= HitWidth)
                health.TakeDamage(Damage * RankMultiplier, caster);
        }
        return true;
    }

    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.LengthSquared();
        if (lengthSquared <= 0.001f) return point.DistanceTo(start);
        float t = Mathf.Clamp((point - start).Dot(segment) / lengthSquared, 0.0f, 1.0f);
        return point.DistanceTo(start + segment * t);
    }
}
