using Godot;

public enum GameState {
	None,
	MainMenu,
	Settings,
	Playing,
};

public partial class StateManager : Node {
	public static StateManager Instance;
	public override void _Ready() {
		Instance = this;
	}

	public GameState State { get; private set; } = GameState.None;
	
	public void ChangeGameState(GameState newState) {
		switch (newState) {
			case GameState.None:
				GetTree().Quit();
				break;
			case GameState.MainMenu:
				Input.SetMouseMode(Input.MouseModeEnum.Visible);
				break;
			case GameState.Settings:
				Input.SetMouseMode(Input.MouseModeEnum.Visible);
				break;
			case GameState.Playing:
				Input.SetMouseMode(Input.MouseModeEnum.Captured);
				break;
			default:
				break;
		}
		
		State = newState;
	}
}
