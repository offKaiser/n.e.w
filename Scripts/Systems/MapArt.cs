using Godot;

/// <summary>Lightweight procedural dressing for the prototype map; decorative only, never blocks a lane.</summary>
public partial class MapArt : Node3D
{
    public override void _Ready()
    {
        RandomNumberGenerator rng = new RandomNumberGenerator { Seed = 241019 }; 
        CreateBiome(new Vector3(-25, 0, -25), new Color(0.72f, 0.9f, 1.0f), new Color(0.35f, 0.55f, 0.68f), rng, true);
        CreateBiome(new Vector3(25, 0, -25), new Color(0.9f, 0.63f, 0.24f), new Color(0.48f, 0.2f, 0.05f), rng, false);
        CreateBiome(new Vector3(-25, 0, 25), new Color(0.14f, 0.5f, 0.2f), new Color(0.08f, 0.22f, 0.1f), rng, false);
        CreateBiome(new Vector3(25, 0, 25), new Color(0.45f, 0.08f, 0.8f), new Color(0.12f, 0.01f, 0.28f), rng, true);
    }

    private void CreateBiome(Vector3 center, Color accent, Color trunk, RandomNumberGenerator rng, bool emissive)
    {
        StandardMaterial3D accentMaterial = new StandardMaterial3D { AlbedoColor = accent, Roughness = 0.65f, EmissionEnabled = emissive, Emission = accent * 0.45f };
        StandardMaterial3D trunkMaterial = new StandardMaterial3D { AlbedoColor = trunk, Roughness = 0.95f };
        for (int i = 0; i < 22; i++)
        {
            Vector3 position;
            do position = center + new Vector3(rng.RandfRange(-21, 21), 0, rng.RandfRange(-17, 17));
            while (Mathf.Abs(position.Z) < 7.0f); // preserves the middle lane.

            bool tree = i % 2 == 0;
            Mesh mesh = tree
                ? new CylinderMesh { TopRadius = rng.RandfRange(0.35f, 0.7f), BottomRadius = rng.RandfRange(0.55f, 0.95f), Height = rng.RandfRange(1.4f, 3.0f) }
                : new SphereMesh { Radius = rng.RandfRange(0.35f, 0.8f), Height = rng.RandfRange(0.7f, 1.5f) };
            AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = tree ? accentMaterial : trunkMaterial, Position = position + Vector3.Up * (tree ? 0.9f : 0.35f), Rotation = new Vector3(0, rng.RandfRange(0, Mathf.Tau), 0) });
        }

        CylinderMesh shrineMesh = new CylinderMesh { TopRadius = 1.15f, BottomRadius = 1.5f, Height = 0.35f };
        AddChild(new MeshInstance3D { Mesh = shrineMesh, MaterialOverride = accentMaterial, Position = center + Vector3.Up * 0.18f });
    }
}
