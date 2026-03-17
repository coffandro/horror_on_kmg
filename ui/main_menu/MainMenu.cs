using Godot;

public partial class MainMenu : PanelContainer {
	[Export(PropertyHint.File, "*.tscn,")] public string loadPath;

	public void _on_play_button_pressed() {
		LoadingScreen.Instance.LoadScene(loadPath);
	}

	public void _on_settings_button_pressed() {

	}

	public void _on_quit_button_pressed() {
		GetTree().Quit();
	}
}
