using Godot;

/// <summary>Short-lived directional visual used for chains and linear cannon shots.</summary>
public partial class BeamVfx : Node3D
{
    private float _remaining;

    public void Configure(Vector3 from, Vector3 to, Color color, float width, float duration)
    {
        Vector3 delta = to - from;
        float length = delta.Length();
        GlobalPosition = (from + to) * 0.5f;
        LookAt(to, Vector3.Up, true);
        CylinderMesh mesh = new CylinderMesh { TopRadius = width, BottomRadius = width, Height = length };
        StandardMaterial3D material = new StandardMaterial3D { AlbedoColor = color, EmissionEnabled = true, Emission = color, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
        AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Rotation = new Vector3(Mathf.Pi * 0.5f, 0, 0) });
        _remaining = duration;
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0) QueueFree();
    }
}
