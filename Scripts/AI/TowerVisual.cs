using Godot;

/// <summary>Presentation-only obelisk tower; combat and collision stay on TowerController.</summary>
public partial class TowerVisual : Node3D
{
    public override void _Ready()
    {
        TowerController tower = GetParent<TowerController>();
        tower.GetNodeOrNull<MeshInstance3D>("MeshInstance3D")?.Hide();
        bool blue = tower.Team == MinionTeam.Blue;
        Color stone = new Color(0.10f, 0.13f, 0.19f);
        Color core = blue ? new Color(0.10f, 0.52f, 1.0f) : new Color(1.0f, 0.13f, 0.18f);
        StandardMaterial3D stoneMaterial = new StandardMaterial3D { AlbedoColor = stone, Metallic = 0.35f, Roughness = 0.62f };
        StandardMaterial3D coreMaterial = new StandardMaterial3D { AlbedoColor = core, EmissionEnabled = true, Emission = core, Metallic = 0.2f, Roughness = 0.2f };

        AddPart(new CylinderMesh { TopRadius = 1.0f, BottomRadius = 1.5f, Height = 0.45f }, new Vector3(0, 0.23f, 0), stoneMaterial);
        AddPart(new CylinderMesh { TopRadius = 0.52f, BottomRadius = 0.86f, Height = 2.75f }, new Vector3(0, 1.75f, 0), stoneMaterial);
        AddPart(new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.42f, Height = 1.45f }, new Vector3(0, 3.82f, 0), coreMaterial);
        AddPart(new SphereMesh { Radius = 0.24f, Height = 0.48f }, new Vector3(0, 3.48f, 0), coreMaterial);
    }

    private void AddPart(PrimitiveMesh mesh, Vector3 position, Material material)
    {
        AddChild(new MeshInstance3D { Mesh = mesh, Position = position, MaterialOverride = material });
    }
}
