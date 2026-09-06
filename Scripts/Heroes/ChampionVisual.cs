using Godot;

public enum ChampionVisualType
{
    NyrVela,
    Nyxara
}

public partial class ChampionVisual : Node3D
{
    [Export]
    public ChampionVisualType Champion = ChampionVisualType.NyrVela;

    private Sprite3D _portrait;
    private MeshInstance3D _leftLeg;
    private MeshInstance3D _rightLeg;
    private float _walkPhase;
    private const float PortraitHeight = 2.2f;

    public override void _Ready()
    {
        GetParent().GetNodeOrNull<MeshInstance3D>("MeshInstance3D")?.Hide();
        if (Champion == ChampionVisualType.NyrVela)
        {
            BuildNyrVela();
        }
        else
        {
            BuildNyxara();
        }
        BuildLegs();
    }

    private void BuildNyrVela()
    {
        AddConceptSprite("res://Assets/Textures/Champions/nyr_vela_concept.png");
    }

    private void BuildNyxara()
    {
        AddConceptSprite("res://Assets/Textures/Champions/nyxara_concept.png");
    }

    private void AddConceptSprite(string texturePath)
    {
        Texture2D texture = ResourceLoader.Load<Texture2D>(texturePath);
        if (texture == null)
        {
            return;
        }

        _portrait = new Sprite3D
        {
            Texture = texture,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.0032f,
            Position = new Vector3(0.0f, PortraitHeight, 0.02f),
            NoDepthTest = false
        };
        AddChild(_portrait);
    }

    public override void _Process(double delta)
    {
        CharacterBody3D body = GetParent() as CharacterBody3D;
        float movement = body == null ? 0.0f : new Vector2(body.Velocity.X, body.Velocity.Z).Length();
        bool walking = movement > 0.08f;
        if (walking) _walkPhase += (float)delta * Mathf.Lerp(7.0f, 13.0f, Mathf.Clamp(movement / 8.0f, 0.0f, 1.0f));

        float swing = walking ? Mathf.Sin(_walkPhase) * 0.48f : 0.0f;
        if (_leftLeg != null) _leftLeg.Rotation = new Vector3(swing, 0, 0);
        if (_rightLeg != null) _rightLeg.Rotation = new Vector3(-swing, 0, 0);
        if (_portrait != null)
        {
            Vector3 position = _portrait.Position;
            position.Y = PortraitHeight + (walking ? Mathf.Abs(Mathf.Sin(_walkPhase * 2.0f)) * 0.055f : 0.0f);
            _portrait.Position = position;
        }
    }

    private void BuildLegs()
    {
        Color color = Champion == ChampionVisualType.NyrVela
            ? new Color(0.11f, 0.025f, 0.22f)
            : new Color(0.08f, 0.035f, 0.14f);
        Color emission = Champion == ChampionVisualType.NyrVela
            ? new Color(0.22f, 0.015f, 0.52f)
            : new Color(0.4f, 0.03f, 0.65f);
        StandardMaterial3D material = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.42f, EmissionEnabled = true, Emission = emission };
        CylinderMesh mesh = new CylinderMesh { TopRadius = 0.13f, BottomRadius = 0.17f, Height = 0.9f };
        _leftLeg = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = new Vector3(-0.22f, 0.48f, 0.16f) };
        _rightLeg = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = new Vector3(0.22f, 0.48f, 0.16f) };
        AddChild(_leftLeg);
        AddChild(_rightLeg);
    }
}
