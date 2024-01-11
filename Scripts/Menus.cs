using System;
using Godot;

public partial class Menus : Control {

    public enum GameState {
        MainMenu,
        Running,
        SettingsMenu,
        PauseMenu
    }


    private RandomNumberGenerator RNG;
    private GameState gameState;
    private Control mainMenu;
    private Control settingsMenu;
    private Control pauseMenu;
    private Button explore;
    private Button settingsMain;
    private Button exit;
    private Button resume;
    private Button settingsPause;
    private Button exitPause;
    private Button back;
    private HSlider renderDistanceSlider;
    private HSlider moveSpeedSlider;
    private PackedScene overlordPS;
    private Overlord overlord;
    private GameState prevGameState;
    public GameState _gameState;


    public Menus() {
        RNG = new RandomNumberGenerator();
    }


    public override void _Ready() {
        this.ProcessMode = ProcessModeEnum.WhenPaused;
        GetTree().Paused = true;
        gameState = GameState.MainMenu;

        mainMenu = this.GetNode<Control>("MainMenu");
        settingsMenu = this.GetNode<Control>("Settings");
        pauseMenu = this.GetNode<Control>("Pause");

        mainMenu.GetNode<Button>("Explore").Pressed += OnExplorePressed;
        mainMenu.GetNode<Button>("Exit").Pressed += OnExitPressed;

        renderDistanceSlider = settingsMenu.GetNode<HSlider>("RenderDistance");
        renderDistanceSlider.DragEnded += OnRenderDistanceDragEnded;
        moveSpeedSlider= settingsMenu.GetNode<HSlider>("MoveSpeed");
        moveSpeedSlider.DragEnded += OnMoveSpeedChanged;
        settingsMenu.GetNode<Button>("Back").Pressed += OnBackPressed;
        settingsMenu.GetNode<Button>("Randomize").Pressed += OnRandomizePressed;

        pauseMenu.GetNode<Button>("Resume").Pressed += OnResumePressed;
        pauseMenu.GetNode<Button>("Settings").Pressed += OnSettingsPressed;
        pauseMenu.GetNode<Button>("Exit").Pressed += OnExitPressed;

        overlordPS = GD.Load<PackedScene>("res://Scenes/Overlord.tscn");
    }


    public override void _Process(double delta) {
        if (_gameState != gameState) {
            gameState = _gameState;
            switch (gameState) {
                case GameState.MainMenu:
                    mainMenu.Visible = true;
                    settingsMenu.Visible = false;
                    pauseMenu.Visible = false;
                    break;
                case GameState.Running:
                    mainMenu.Visible = false;
                    settingsMenu.Visible = false;
                    pauseMenu.Visible = false;

                    Input.MouseMode = Input.MouseModeEnum.Captured;

                    GetTree().Paused = false;
                    break;
                case GameState.SettingsMenu:
                    mainMenu.Visible = false;
                    settingsMenu.Visible = true;
                    pauseMenu.Visible = false;
                    break;
                case GameState.PauseMenu:
                    mainMenu.Visible = false;
                    settingsMenu.Visible = false;
                    pauseMenu.Visible = true;

                    Input.MouseMode = Input.MouseModeEnum.Visible;

                    GetTree().Paused = true;
                    break;
            }

        }
    }


    private void OnExplorePressed() {
        overlord = overlordPS.Instantiate<Overlord>();
        overlord.ProcessMode = ProcessModeEnum.Pausable;
        overlord.GamePauseToggled += OnEscapePressed;
        this.AddChild(overlord);
        _gameState = GameState.Running;
    }

    private void OnSettingsPressed() {
        prevGameState = gameState;
        _gameState = GameState.SettingsMenu;
    }

    private void OnResumePressed() {
        _gameState = GameState.Running;
    }

    private void OnBackPressed() {
        _gameState = prevGameState;
    }

    private void OnExitPressed() {
        GetTree().Quit();
    }

    private void OnRenderDistanceDragEnded(bool valueChanged) {
        overlord._renderDistance = (byte)renderDistanceSlider.Value;
    }

    private void OnEscapePressed() {
        GetTree().Paused = true;
        _gameState = GameState.PauseMenu;
    }

    private void OnRandomizePressed() {
        overlord.QueueFree();
        overlord = overlordPS.Instantiate<Overlord>();
        overlord.ProcessMode = ProcessModeEnum.Pausable;
        overlord.GamePauseToggled += OnEscapePressed;
        overlord.SetSeed((int)RNG.Randi());
        this.AddChild(overlord);
        _gameState = GameState.Running;
    }

    private void OnMoveSpeedChanged(bool valueChanged) {
        if (valueChanged) overlord.SetPlayerSpeed(moveSpeedSlider.Value);
    }


    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey eventKey) {
            if (eventKey.Pressed && eventKey.KeyLabel == Key.Escape)
                if (gameState == GameState.PauseMenu) _gameState = GameState.Running;
        }
    }
}