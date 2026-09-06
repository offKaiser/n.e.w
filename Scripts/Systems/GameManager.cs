using Godot;

public partial class GameManager : Node
{
    [Export]
    public NodePath HeroPath;

    [Export]
    public NodePath BlueTowerPath;

    [Export]
    public NodePath RedTowerPath;

    [Export]
    public NodePath StatusLabelPath;

    private HealthComponent _heroHealth;
    private HealthComponent _blueTowerHealth;
    private HealthComponent _redTowerHealth;
    private Label _statusLabel;
    private bool _gameFinished;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _heroHealth = GetNode<Node>(HeroPath).GetNode<HealthComponent>("HealthComponent");
        _blueTowerHealth = GetNode<Node>(BlueTowerPath).GetNode<HealthComponent>("HealthComponent");
        _redTowerHealth = GetNode<Node>(RedTowerPath).GetNode<HealthComponent>("HealthComponent");
        _statusLabel = GetNodeOrNull<Label>(StatusLabelPath);
    }

    public override void _Process(double delta)
    {
        if (_gameFinished)
        {
            return;
        }

        if (!_heroHealth.IsAlive)
        {
            FinishGame("DERROTA - Hero derrotado");
        }
        else if (!_blueTowerHealth.IsAlive)
        {
            FinishGame("DERROTA - Torre azul destruida");
        }
        else if (!_redTowerHealth.IsAlive)
        {
            FinishGame("VITORIA - Torre vermelha destruida");
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_gameFinished || !Input.IsActionJustPressed("restart_game"))
        {
            return;
        }

        GetTree().Paused = false;
        GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
    }

    private void FinishGame(string result)
    {
        _gameFinished = true;
        if (_statusLabel != null)
        {
            _statusLabel.Text = $"{result}\nPressione Enter para reiniciar";
            _statusLabel.Visible = true;
        }

        GetTree().Paused = true;
    }
}
