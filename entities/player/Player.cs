using Godot;
using System;

public partial class Player : CharacterBody3D {
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	[Export] public float moveSpeed = 5.0f;
	[Export] public float jumpVelocity = 8f;
	[Export] public float mouseSensitivity = 700;
	[Export] public float gamepadSensitivity = 0.075f;

	[ExportGroup("Components")]
	[Export] public Camera3D camera;
	[Export] public SubViewport subViewport;
	[Export] public Hud hud;
	[Export] public Pause pause;

	[ExportGroup("Pause Camera settings")]
	[Export] public Vector2 pausePhoneScreenPosition = new Vector2(0.85f, 0.5f);
	[Export] public float pausePhoneLerpSpeed = 5.0f;

	[ExportGroup("Hud Camera settings")]
	[Export] public Vector2 hudPhoneScreenPosition = new Vector2(0.85f, 0.75f);
	[Export] public float hudPhoneLerpSpeed = 7.5f;

	private Vector3 _movementVelocity;
	private Vector3 _rotationTarget;

	private Vector2 _inputMouse;

	private int _health = 100;
	private float _gravity;

	private bool _jumping = false;

	public override void _Ready() {
		base._Ready();

		camera.Current = true;
		Phone.Instance.ResetSettings();
		Phone.Instance.camera = camera;
		Phone.Instance.TurnLight(true);
		Phone.Instance.SetSubviewPort(subViewport);

		SwitchPhoneMode(false);
	}

	public override void _PhysicsProcess(double delta) {
		// Handle functions
		HandleControls((float)delta);
		HandleGravity((float)delta);

		// Movement
		_movementVelocity = Transform.Basis * _movementVelocity;

		Vector3 appliedVelocity = Velocity.Lerp(_movementVelocity, (float)delta * 10);
		appliedVelocity.Y = -_gravity;

		Velocity = appliedVelocity;
		MoveAndSlide();

		// Return state
		if (IsOnFloor() && _jumping) {
			_jumping = false;
		}
	}

	public override void _Input(InputEvent @event) {
		base._Input(@event);

		if (@event is InputEventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured) {
			InputEventMouseMotion motionEven = (InputEventMouseMotion)@event;
			_inputMouse = motionEven.Relative / mouseSensitivity;
			HandleRotation(motionEven.Relative.X, motionEven.Relative.Y);
		}
	}

	public void SwitchPhoneMode(bool paused) {
		if (paused) {
			GD.Print("Switching to paused");
			pause.Show();
			hud.Hide();

			Phone.Instance.LerpSpeed = pausePhoneLerpSpeed;
			Phone.Instance.ScreenPosition = pausePhoneScreenPosition;

			StateManager.Instance.ChangeGameState(GameState.Paused);
		} else {
			GD.Print("Switching to playing");
			pause.Hide();
			hud.Show();

			Phone.Instance.LerpSpeed = hudPhoneLerpSpeed;
			Phone.Instance.ScreenPosition = hudPhoneScreenPosition;

			StateManager.Instance.ChangeGameState(GameState.Playing);
		}
	}

	public void _on_return_button_pressed() {
		SwitchPhoneMode(false);
	}

	private void HandleControls(float delta) {
		// Mouse Capture
		if (Input.IsActionJustPressed("mouse_capture_exit") && Input.MouseMode == Input.MouseModeEnum.Captured) {
			_inputMouse = Vector2.Zero;
			SwitchPhoneMode(true);
		}

		// Movement
		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		_movementVelocity = new Vector3(
			input.X,
			0,
			input.Y
		).Normalized() * moveSpeed;

		// Handle controller rotation
		Vector2 rotationInput = Input.GetVector("camera_right", "camera_left", "camera_down", "camera_up");
		if (rotationInput != Vector2.Zero) {
			HandleRotation(rotationInput.X, rotationInput.Y, delta);
		}

		if (Input.IsActionJustPressed("jump")) {
			ActionJump();
		}
	}

	public void HandleRotation(float xRot, float yRot) {
		_rotationTarget += new Vector3(
			-yRot,
			-xRot,
			0
		) / mouseSensitivity;

		_rotationTarget.X = Mathf.Clamp(
			_rotationTarget.X,
			Mathf.DegToRad(-90),
			Mathf.DegToRad(90)
		);

		camera.Rotation = new Vector3(
			_rotationTarget.X,
			camera.Rotation.Y,
			camera.Rotation.Z
		);

		Rotation = new Vector3(
			Rotation.X,
			_rotationTarget.Y,
			Rotation.Z
		);
	}

	public void HandleRotation(float xRot, float yRot, float delta) {
		_rotationTarget -= new Vector3(
			-yRot,
			-xRot,
			0
		).LimitLength(1.0f) * gamepadSensitivity;

		_rotationTarget.X = Mathf.Clamp(
			_rotationTarget.X,
			Mathf.DegToRad(-90),
			Mathf.DegToRad(90)
		);

		camera.Rotation = new Vector3(
			Mathf.LerpAngle(camera.Rotation.X, _rotationTarget.X, delta * 25),
			camera.Rotation.Y,
			camera.Rotation.Z
		);

		Rotation = new Vector3(
			Rotation.X,
			Mathf.LerpAngle(Rotation.Y, _rotationTarget.Y, delta * 25),
			Rotation.Z
		);
	}

	private void HandleGravity(float delta) {
		_gravity += 20 * delta;

		if (_gravity > 0 && IsOnFloor()) {
			_gravity = 0;
		}
	}

	public void ActionJump() {
		if (_jumping) return;

		_gravity = -jumpVelocity;
		_jumping = true;
	}

	public void TeleportTo(Vector3 globalPosition) {
		GlobalPosition = globalPosition;

		// Prevent one-frame carry-over from previous movement after teleport.
		Velocity = Vector3.Zero;
		_movementVelocity = Vector3.Zero;

		ResetPhysicsInterpolation();
		Phone.Instance?.SnapToCurrentTarget();
	}
}
