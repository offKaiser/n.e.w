using Godot;

public partial class CameraFollow : Camera3D
{
    [Export]
    public NodePath TargetPath = new NodePath("../Hero");

    [Export]
    public Vector3 Offset = new Vector3(0.0f, 12.0f, 12.0f);

    [Export]
    public float FollowSpeed = 8.0f;

    private Node3D _target;

    public override void _Ready()
    {
        _target = GetNodeOrNull<Node3D>(TargetPath);
    }

    public override void _Process(double delta)
    {
        if (_target == null)
        {
            return;
        }

        Vector3 targetPosition = _target.GlobalPosition + Offset;
        float interpolation = 1.0f - Mathf.Exp(-FollowSpeed * (float)delta);

        GlobalPosition = GlobalPosition.Lerp(targetPosition, interpolation);
        LookAt(_target.GlobalPosition, Vector3.Up);
    }

    public void SetTarget(Node3D target)
    {
        _target = target;
    }
}
