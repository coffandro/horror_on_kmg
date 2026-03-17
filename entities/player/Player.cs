using Godot;
using System;

public partial class Player : CharacterBody3D {
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	[ExportSubgroup("Properties")]
	[Export] public float moveSpeed = 5.0f;
	[Export] public float jumpVelocity = 8f;
	[Export] public float mouseSensitivity = 700;
	[Export] public float gamepadSensitivity = 0.075f;

	[Export] public Camera3D camera;
	[Export] public SubViewport subViewport;
	[Export] public Vector2 cameraScreenPosition = new Vector2(0.2f, 0.5f);
	[Export] public float cameraDistance = 0.125f;
	[Export] public float rotSpeed = 7.5f;
	[Export] public Vector3 phoneRotation = new Vector3(0, 180, 0);

	private bool mouseCaptured = true;

	private Vector3 movementVelocity;
	private Vector3 rotationTarget;

	private Vector2 inputMouse;
	private Vector3 leftContainerOffset;
	private Vector3 rightContainerOffset;

	private int health = 100;
	private float gravity;

	private bool jumping = false;
	private bool previously_floored = false;

	public override void _Ready() {
		base._Ready();

		camera.Current = true;
		Phone.Instance.camera = camera;
		Phone.Instance.TurnLight(true);
		Phone.Instance.SetSubviewPort(subViewport);
		Phone.Instance.SetViewportRot(0);

		Phone.Instance.rotationSpeed = rotSpeed;
		Phone.Instance.screenPosition = cameraScreenPosition;
		Phone.Instance.rotationOffsetDegrees = phoneRotation;
		Phone.Instance.distanceFromcamera = cameraDistance;

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(double delta) {
		// Handle functions
		HandleControls((float)delta);
		HandleGravity((float)delta);

		// Movement
		movementVelocity = Transform.Basis * movementVelocity;

		Vector3 appliedVelocity = Velocity.Lerp(movementVelocity, (float)delta * 10);
		appliedVelocity.Y = -gravity;

		Velocity = appliedVelocity;
		MoveAndSlide();

		// Return state
		if (IsOnFloor() && jumping) {
			jumping = false;
		}
	}

	public override void _Input(InputEvent @event) {
		base._Input(@event);

		if (@event is InputEventMouseMotion && mouseCaptured) {
			InputEventMouseMotion motionEven = (InputEventMouseMotion)@event;
			inputMouse = motionEven.Relative / mouseSensitivity;
			HandleRotation(motionEven.Relative.X, motionEven.Relative.Y);
		}
	}

	private void HandleControls(float delta) {
		// Mouse Capture
		if (Input.IsActionJustPressed("mouse_capture")) {
			Input.MouseMode = Input.MouseModeEnum.Captured;
			mouseCaptured = true;
		} else if (Input.IsActionJustPressed("mouse_capture_exit")) {
			Input.MouseMode = Input.MouseModeEnum.Visible;
			mouseCaptured = true;

			inputMouse = Vector2.Zero;
		}

		// Movement
		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		movementVelocity = new Vector3(
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
		rotationTarget += new Vector3(
			-yRot,
			-xRot,
			0
		) / mouseSensitivity;

		rotationTarget.X = Mathf.Clamp(
			rotationTarget.X,
			Mathf.DegToRad(-90),
			Mathf.DegToRad(90)
		);

		camera.Rotation = new Vector3(
			rotationTarget.X,
			camera.Rotation.Y,
			camera.Rotation.Z
		);

		Rotation = new Vector3(
			Rotation.X,
			rotationTarget.Y,
			Rotation.Z
		);
	}

	public void HandleRotation(float xRot, float yRot, float delta) {
		rotationTarget -= new Vector3(
			-yRot,
			-xRot,
			0
		).LimitLength(1.0f) * gamepadSensitivity;

		rotationTarget.X = Mathf.Clamp(
			rotationTarget.X,
			Mathf.DegToRad(-90),
			Mathf.DegToRad(90)
		);

		camera.Rotation = new Vector3(
			Mathf.LerpAngle(camera.Rotation.X, rotationTarget.X, delta * 25),
			camera.Rotation.Y,
			camera.Rotation.Z
		);

		Rotation = new Vector3(
			Rotation.X,
			Mathf.LerpAngle(Rotation.Y, rotationTarget.Y, delta * 25),
			Rotation.Z
		);
	}

	private void HandleGravity(float delta) {
		gravity += 20 * delta;

		if (gravity > 0 && IsOnFloor()) {
			gravity = 0;
		}
	}

	public void ActionJump() {
		if (jumping) return;

		gravity = -jumpVelocity;
		jumping = true;
	}
}
