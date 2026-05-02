using Godot;

public partial class LoadingScreen : Node3D {
	public static LoadingScreen Instance { get; private set; }

	[Export] public Camera3D camera;
	[Export] public SubViewport subViewport;
	[Export] public Vector2 cameraScreenPosition = new Vector2(0.5f, 0.5f);
	[Export] public float cameraDistance = 0.15f;
	[Export] public Vector3 phoneRotation = new Vector3(0, 180, 90);

	[Export] public string loadingText = "Loading...";

	[ExportGroup("Internal")]
	[Export] public ProgressBar loadBar;
	[Export] public Label label;

	// State
	private string target;
	private Godot.Collections.Array<Variant> level_data;
	private bool baloonLoadTime = true;
	private bool loaded = false;
	private float managerProgress = 0.0f;

	public override void _Ready() {
		Instance = this; // Singleton pattern

		base._Ready();

		LoadingManager.Instance.ProgressUpdated += UpdateProgress;
		LoadingManager.Instance.Failed += Failed;
		LoadingManager.Instance.Loaded += Loaded;

		Phone.Instance.ReachedRotation += ReachedRotation;

		// Initial values
		label.Text = loadingText;
	}

	void TakePhone() {
		camera.Current = true;
		Phone.Instance.ResetSettings();
		Phone.Instance.camera = camera;
		Phone.Instance.TurnLight(false);
		Phone.Instance.SetSubviewPort(subViewport);
		Phone.Instance.SetViewportRot(90);

		Phone.Instance.ScreenPosition = cameraScreenPosition;
		Phone.Instance.DistanceFromCamera = cameraDistance;
		Phone.Instance.RotationOffsetDegrees = phoneRotation;
	}

	// Begin load using manager
	public void LoadScene(string target) {
		GD.Print("starting ", target);
		baloonLoadTime = true;
		this.target = target;
		TakePhone();
	}

	public void LoadScene(string target, Godot.Collections.Array<Variant> extra_data) {
		GD.Print("starting ", target, " with ", extra_data);
		baloonLoadTime = true;
		this.target = target;
		level_data = extra_data;
		TakePhone();
	}

	// Begin load using manager and enable internal baloonLoadTime
	public void LoadScene(string target, bool loadTimeExtend) {
		GD.Print("starting ", target);
		baloonLoadTime = loadTimeExtend;
		this.target = target;
		TakePhone();
	}

	// We've reached our rotation, we can start load properly
	private void ReachedRotation() {
		// We're in focus
		if (Phone.Instance.camera != camera) {
			return;
		}

		if (level_data != null) {
			LoadingManager.Instance.InitiateSceneLoad(target, this, level_data);
		} else {
			LoadingManager.Instance.InitiateSceneLoad(target, this);
		}
	}

	// Called every frame
	public override void _Process(double delta) {
		base._Process(delta);

		// Don't do anything if not loaded
		if (!LoadingManager.Instance.IsLoading() && !loaded) return;

		if (loaded) {
			if (baloonLoadTime) {
				// If loaded and baloonLoadTime enabled we lerp until 99% before finishing
				loadBar.Value = Mathf.Lerp(loadBar.Value, 100, delta * 15);

				if (loadBar.Value > 99) {
					Finish();
				}
			}
		} else {
			// If not loaded we set our percentage according to what we've been told
			loadBar.Value = Mathf.Lerp(loadBar.Value, managerProgress, delta * 150);
		}
	}

	public void Finish() {
		// Tell manager to finish
		LoadingManager.Instance.Finish(this);

		// Clear state
		managerProgress = 0;
		baloonLoadTime = true;
		loaded = false;
	}

	// Manager signal responses below
	public void UpdateProgress(float progress) {
		// Update manager progress according to newly reported state
		managerProgress = (int)(progress * 100);
	}

	public void Failed(string error) {
		GD.Print("Loading failed " + error);

		// display error, hide navbar and stop loading
		label.Text = "Loading failed " + error;
		loadBar.Hide();
	}

	// Set loaded variable
	public void Loaded() {
		if (!baloonLoadTime) {
			// If loaded and baloonLoadTime disbled we finish immedietly
			Finish();
			return;
		}

		// Otherwise we set the variabel for process to update progress
		loaded = true;
	}
}
