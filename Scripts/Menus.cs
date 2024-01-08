using System.Collections.Generic;
using Godot;

public partial class Menus : Control {

    public enum GameState {
        MainMenu,
        Running,
        SettingsMenu,
        PauseMenu
    }


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
    private Overlord overlord;
    private GameState prevGameState;

    public GameState _gameState;


    public override void _Ready() {
        this.ProcessMode = ProcessModeEnum.WhenPaused;
        GetTree().Paused = true;
        gameState = GameState.MainMenu;

        mainMenu = this.GetNode<Control>("MainMenu");
        settingsMenu = this.GetNode<Control>("Settings");
        pauseMenu = this.GetNode<Control>("Pause");

        mainMenu.GetNode<Button>("Explore").Pressed += OnExplorePressed;
        mainMenu.GetNode<Button>("Settings").Pressed += OnSettingsPressed;
        mainMenu.GetNode<Button>("Exit").Pressed += OnExitPressed;

        renderDistanceSlider = settingsMenu.GetNode<HSlider>("RenderDistance");
        renderDistanceSlider.DragEnded += OnRenderDistanceDragEnded;
        settingsMenu.GetNode<Button>("Back").Pressed += OnBackPressed;

        pauseMenu.GetNode<Button>("Resume").Pressed += OnResumePressed;
        pauseMenu.GetNode<Button>("Settings").Pressed += OnSettingsPressed;
        pauseMenu.GetNode<Button>("Exit").Pressed += OnExitPressed;

        overlord = GD.Load<PackedScene>("res://Scenes/Overlord.tscn").Instantiate<Overlord>();
        overlord.ProcessMode = ProcessModeEnum.Pausable;
        overlord.GamePauseToggled += OnEscapePressed;
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

                    GetTree().Paused = true;
                    break;
            }

        }
    }


    private void OnExplorePressed() {
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
}