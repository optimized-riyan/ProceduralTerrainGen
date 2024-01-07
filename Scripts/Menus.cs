using Godot;

public partial class Menus : Control {

	enum GameState {
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
	private HSlider renderDistance;


    public override void _Ready() {
		gameState = GameState.MainMenu;

		mainMenu = this.GetNode<Control>("MainMenu");
		settingsMenu = this.GetNode<Control>("Settings");
		pauseMenu = this.GetNode<Control>("Pause");

		mainMenu.GetNode<Button>("Explore").Pressed += OnExplorePressed;
		mainMenu.GetNode<Button>("Settings").Pressed += OnSettingsPressed;
		mainMenu.GetNode<Button>("Exit").Pressed += OnExitPressed;

		settingsMenu.GetNode<HSlider>("RenderDistance").DragEnded += OnRenderDistanceDragEnded;
		settingsMenu.GetNode<Button>("Back").Pressed += OnBackPressed;

		pauseMenu.GetNode<Button>("Resume").Pressed += OnResumePressed;
		pauseMenu.GetNode<Button>("Settings").Pressed += OnSettingsPressed;
		pauseMenu.GetNode<Button>("Exit").Pressed += OnExitPressed;
    }


    public override void _Process(double delta) {
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
				break;
		}
    }


    private void OnExplorePressed() {
		this.AddChild(GD.Load<PackedScene>("res://Scenes/Overlord.tscn").Instantiate<Overlord>());
		gameState = GameState.Running;
	}

	private void OnSettingsPressed() {
		gameState = GameState.SettingsMenu;
	}

	private void OnResumePressed() {
		gameState = GameState.Running;
	}

	private void OnBackPressed() {
		gameState = GameState.PauseMenu;
	}

	private void OnExitPressed() {
		
	}

	private void OnRenderDistanceDragEnded(bool valueChanged) {

	}
}