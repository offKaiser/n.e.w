using Godot;

/// <summary>Three-second mark applied by an empowered Echo do Abismo hit.</summary>
public partial class AbyssMark : Node
{
    private NodePath _casterPath;
    private double _expiresAt;

    public void Apply(Node3D caster, float duration)
    {
        _casterPath = caster.GetPath();
        _expiresAt = Time.GetTicksMsec() / 1000.0 + duration;
    }

    public override void _Process(double delta)
    {
        if (Time.GetTicksMsec() / 1000.0 >= _expiresAt) QueueFree();
    }

    public void RewardIfKilledBy(Node source)
    {
        if (Time.GetTicksMsec() / 1000.0 >= _expiresAt) return;
        Node caster = GetTree().Root.GetNodeOrNull<Node>(_casterPath);
        for (Node current = source; current != null; current = current.GetParent())
        {
            if (current == caster)
            {
                (caster as HeroController)?.ReduceAbilityCooldowns(1.5f);
                return;
            }
        }
    }
}
