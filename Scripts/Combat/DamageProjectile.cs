using Godot;

public partial class DamageProjectile : Node3D
{
    private Node3D _target;
    private HealthComponent _targetHealth;
    private Node _source;
    private float _damage;
    private float _speed;
    private Color _color;
    private ProjectileVisualType _visualType = ProjectileVisualType.GenericMagic;

    public void Configure(Node3D target, HealthComponent targetHealth, Node source, float damage, float speed, Color color, ProjectileVisualType visualType = ProjectileVisualType.GenericMagic)
    {
        _target = target;
        _targetHealth = targetHealth;
        _source = source;
        _damage = damage;
        _speed = speed;
        _color = color;
        _visualType = visualType;
    }

    public override void _Ready()
    {
        if (_source is Node3D source) NetworkPresentationReplicator.PublishProjectileLaunch(source, _target, _visualType, _speed, _color);
        SphereMesh mesh = new SphereMesh { Radius = 0.16f, Height = 0.32f };
        StandardMaterial3D material = new StandardMaterial3D
        {
            AlbedoColor = _color,
            EmissionEnabled = true,
            Emission = _color
        };
        MeshInstance3D visual = new MeshInstance3D { Mesh = mesh, MaterialOverride = material };
        AddChild(visual);
    }

    public override void _Process(double delta)
    {
        if (_source is Node sourceNode)
        {
            HealthComponent sourceHealth = sourceNode.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (sourceHealth != null && !sourceHealth.IsAlive)
            {
                QueueFree();
                return;
            }
        }

        if (_target == null || !GodotObject.IsInstanceValid(_target) || _targetHealth == null || !_targetHealth.IsAlive)
        {
            QueueFree();
            return;
        }

        Vector3 targetPosition = _target.GlobalPosition + Vector3.Up * 0.9f;
        Vector3 offset = targetPosition - GlobalPosition;
        float travelDistance = _speed * (float)delta;
        if (offset.LengthSquared() <= travelDistance * travelDistance)
        {
            _targetHealth.TakeDamage(_damage, _source);
            if (_source is Node3D source) NetworkPresentationReplicator.PublishProjectileImpact(source, _target, _visualType, targetPosition);
            QueueFree();
            return;
        }

        GlobalPosition += offset.Normalized() * travelDistance;
    }
}
