using Godot;

/// <summary>Client-only transient projectile drawing; it has no collision or gameplay state.</summary>
public partial class ProjectilePresentationVisual : Node3D
{
    private Vector3 _start;
    private Vector3 _end;
    private float _duration;
    private float _elapsed;

    public void Configure(Vector3 start, Vector3 end, float duration, Color color)
    {
        _start = start; _end = end; _duration = Mathf.Max(duration, 0.05f);
        SphereMesh mesh = new SphereMesh { Radius = 0.16f, Height = 0.32f };
        StandardMaterial3D material = new StandardMaterial3D { AlbedoColor = color, EmissionEnabled = true, Emission = color };
        AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material });
        GlobalPosition = start;
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        GlobalPosition = _start.Lerp(_end, Mathf.Clamp(_elapsed / _duration, 0.0f, 1.0f));
        if (_elapsed >= _duration) QueueFree();
    }
}
