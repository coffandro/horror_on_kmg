using Godot;

public partial class Main : Node3D {
	[Export] public Camera3D camera;
	[Export] public SubViewport subViewport;
	[Export] public Vector2 cameraScreenPosition = new Vector2(0.2f, 0.5f);
	[Export] public float cameraDistance = 0.15f;

	public override void _Ready() {
		Phone.Instance.ResetSettings();
		Phone.Instance.camera = camera;
		Phone.Instance.TurnLight(false);
		Phone.Instance.SetSubviewPort(subViewport);

		Phone.Instance.ScreenPosition = cameraScreenPosition;
		Phone.Instance.DistanceFromCamera = cameraDistance;
	}
}
