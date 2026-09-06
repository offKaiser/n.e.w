using Godot;

public partial class TimedVfx : Node3D
{
    private float _duration;
    private float _elapsed;
    private Vector3 _initialScale;

    public void Configure(Color color, float radius, float duration)
    {
        _duration = duration;
        SphereMesh mesh = new SphereMesh { Radius = radius, Height = radius * 2.0f };
        StandardMaterial3D material = new StandardMaterial3D { AlbedoColor = color, EmissionEnabled = true, Emission = color, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        MeshInstance3D visual = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Scale = Vector3.One * 0.2f };
        AddChild(visual);
        _initialScale = Vector3.One * 0.2f;
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float progress = Mathf.Clamp(_elapsed / _duration, 0.0f, 1.0f);
        Scale = _initialScale.Lerp(Vector3.One * 1.6f, progress);
        if (progress >= 1.0f)
        {
            QueueFree();
        }
    }
}
