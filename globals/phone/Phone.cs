using System;
using System.Linq;
using Godot;

public enum ProcessingTypeEnum {
	PROCESS,
	PHYSICS
}

public struct PhoneSettings {
	public PhoneSettings() {

	}

	public Vector2 ScreenPosition = new Vector2(0.8f, 0.8f);
	public Vector3 RotationOffsetDegrees = new Vector3(0, 180, 0);

	// normalized screen position (0-1)
	[Export] public float distanceFromCamera = 0.25f;
	[Export] public float lerpSpeed = 5.0f;
	[Export] public float rotationSpeed = 7.5f;
	// 0 = phone matches camera orientation, 1 = phone faces camera center
	[Export] public float lookAtCameraStrength = 1.0f;

	// Max screen-space drift from screenPosition (normalized, 0 = unlimited)
	// e.g. 0.05 means the phone can't drift more than 5% of the screen from its anchor
	[Export] public float maxScreenDrift = 0.05f;
	[Export] public bool deepthroatingInput = true;
}


public partial class Phone : Node3D {
	public static Phone Instance { get; private set; }

	[Signal] public delegate void ReachedPositionEventHandler();
	[Signal] public delegate void ReachedRotationEventHandler();

	[Export] public Camera3D camera;

	private PhoneSettings _settings;
	private PhoneSettings _defaultSettings;

	[Export]
	public Vector2 ScreenPosition {
		get {
			return _settings.ScreenPosition;
		}
		set {
			reachedPosition = false;

			_settings.ScreenPosition = value;
		}
	}


	// Rotation offset relative to camera
	[Export]
	public Vector3 RotationOffsetDegrees {
		get {
			return _settings.RotationOffsetDegrees;
		}
		set {
			reachedRotation = false;

			_settings.RotationOffsetDegrees = value;
		}
	}

	[Export]
	public float RotationSpeed {
		get {
			return _settings.rotationSpeed;
		}
		set {
			_settings.rotationSpeed = value;
		}
	}

	[Export]
	public float LerpSpeed {
		get {
			return _settings.lerpSpeed;
		}
		set {
			_settings.lerpSpeed = value;
		}
	}

	[Export]
	public float DistanceFromCamera {
		get {
			return _settings.distanceFromCamera;
		}
		set {
			_settings.distanceFromCamera = value;
		}
	}

	[ExportGroup("Internal")]
	[Export] public Node3D phone;
	[Export] public MeshInstance3D quadMesh;
	[Export] public MeshInstance3D phoneMesh;
	[Export] public Node3D light;
	[Export] public ProcessingTypeEnum processProcess;
	[Export] public Node3D debugPointer;

	// internal state
	private SubViewport subViewport;
	private bool mouseInViewport = false;
	private Vector2? lastEventPos2D;
	private float lastEventTime = -1.0f;

	private bool reachedPosition, reachedRotation;

	public override void _Ready() {
		Instance = this;

		_defaultSettings = new PhoneSettings();

		ResetSettings();
	}

	// TODO: Add default settings from ready function, reset for every change og ctx
	public void ResetSettings() {
		_settings = _defaultSettings;

		SetViewportRot(0);
		TurnLight(true);
	}

	public void TurnLight(bool enabled) {
		light.Visible = enabled;
	}

	public void SetViewportRot(float rot) {
		phoneMesh.GetSurfaceOverrideMaterial(2).Set("shader_parameter/rotation_degrees", rot);
	}

	public void SetSubviewPort(SubViewport subViewport) {
		this.subViewport = subViewport;

		var tex = subViewport.GetTexture();

		phoneMesh.GetSurfaceOverrideMaterial(2).Set("shader_parameter/albedo_texture", tex);
	}

	public override void _UnhandledInput(InputEvent @event) {
		base._UnhandledInput(@event);

		if (_settings.deepthroatingInput) {
			Type type = @event.GetType();
			// We handle these specially later
			Type[] ignoreTypes = [
				typeof(InputEventMouseButton),
				typeof(InputEventMouseMotion)
			];

			if (ignoreTypes.Contains(type)) {
				return;
			}

			subViewport.PushInput(@event);
		}
	}

	public void _on_area_3d_input_event(Camera3D _camera, InputEvent inputEvent, Vector3 eventPosition, Vector3 _normal, int _shapeIdx) {
		// Get mesh size to detect edges and make conversions. This code only supports PlaneMesh and QuadMesh.
		Vector2 quadMeshSize = ((QuadMesh)quadMesh.Mesh).Size;

		// Event position in Area3D in world coordinate space.
		Vector3 eventPos3D = eventPosition;

		// Current time in seconds since engine start.
		float now = Time.GetTicksMsec() / 1000.0f;

		// Convert position to a coordinate space relative to the Area3D node.
		// NOTE: AffineInverse() accounts for the Area3D node's scale, rotation, and position in the scene!
		eventPos3D = quadMesh.GlobalTransform.AffineInverse() * eventPos3D;

		Vector2 eventPos2D = Vector2.Zero;

		if (mouseInViewport) {
			// Convert the relative event position from 3D to 2D.
			eventPos2D = new Vector2(eventPos3D.X, -eventPos3D.Y);

			// Right now the event position's range is the following: (-quad_size/2) -> (quad_size/2)
			// We need to convert it into the following range: -0.5 -> 0.5
			eventPos2D.X /= quadMeshSize.X;
			eventPos2D.Y /= quadMeshSize.Y;
			// Then we need to convert it into the following range: 0 -> 1
			eventPos2D.X += 0.5f;
			eventPos2D.Y += 0.5f;

			// Finally, we convert the position to the following range: 0 -> viewport.size
			eventPos2D.X *= subViewport.Size.X;
			eventPos2D.Y *= subViewport.Size.Y;
			// We need to do these conversions so the event's position is in the viewport's coordinate system.
		} else if (lastEventPos2D.HasValue) {
			// Fall back to the last known event position.
			eventPos2D = lastEventPos2D.Value;
		}

		// Set the event's position and global position.
		inputEvent.Set("position", eventPos2D);
		if (inputEvent is InputEventMouse mouseEvent) {
			mouseEvent.GlobalPosition = eventPos2D;
		}

		// Calculate the relative event distance.
		if (inputEvent is InputEventMouseMotion motionEvent) {
			if (!lastEventPos2D.HasValue) {
				motionEvent.Relative = Vector2.Zero;
			} else {
				motionEvent.Relative = eventPos2D - lastEventPos2D.Value;
				motionEvent.Velocity = motionEvent.Relative / (float)(now - lastEventTime);
			}
		} else if (inputEvent is InputEventScreenDrag dragEvent) {
			if (!lastEventPos2D.HasValue) {
				dragEvent.Relative = Vector2.Zero;
			} else {
				dragEvent.Relative = eventPos2D - lastEventPos2D.Value;
				dragEvent.Velocity = dragEvent.Relative / (float)(now - lastEventTime);
			}
		}

		// Update last_event_pos2D with the position we just calculated.
		lastEventPos2D = eventPos2D;

		// Update last_event_time to current time.
		lastEventTime = now;

		// Finally, send the processed input event to the viewport.
		subViewport.PushInput(inputEvent);
	}

	public void _on_area_3d_mouse_entered() {
		subViewport.Notification((int)NotificationVpMouseEnter);
		mouseInViewport = true;
	}

	public void _on_area_3d_mouse_exited() {
		subViewport.Notification((int)NotificationVpMouseExit);
		mouseInViewport = false;
	}

	// Processing :o
	public override void _Process(double delta) {
		base._Process(delta);
		if (processProcess == ProcessingTypeEnum.PROCESS) {
			Update((float)delta);
		}
	}

	public override void _PhysicsProcess(double delta) {
		base._PhysicsProcess(delta);
		if (processProcess == ProcessingTypeEnum.PHYSICS) {
			Update((float)delta);
		}
	}

	private void Update(float delta) {
		if (camera == null || phone == null) {
			GD.Print("camera or phone missing");
			return;
		}

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

		// Convert normalized screen pos to pixels
		Vector2 screenPixel = new Vector2(
			ScreenPosition.X * viewportSize.X,
			ScreenPosition.Y * viewportSize.Y
		);

		// Get ray direction from camera through screen position
		Vector3 rayDir = camera.ProjectRayNormal(screenPixel);

		// Target position in world space (from camera position, not near plane)
		Vector3 targetPos = camera.GlobalPosition + rayDir * _settings.distanceFromCamera;

		// Target rotation: blend between camera orientation and facing camera center
		Basis cameraBasis = camera.GlobalBasis;
		Quaternion cameraQuat = new Quaternion(cameraBasis);
		Basis offsetBasis = Basis.FromEuler(
			RotationOffsetDegrees * Mathf.DegToRad(1.0f)
		);
		Basis finalBasis = new Basis(cameraQuat) * offsetBasis;
		Quaternion targetRot = new Quaternion(finalBasis);

		//debugPointer.GlobalPosition =  targetPos;

		// Smooth position (exponential decay — higher lerpSpeed = faster catch-up, no overshoot)
		float posFactor = 1f - Mathf.Exp(-_settings.lerpSpeed * delta);
		Vector3 newPos = posFactor >= 0.999f
			? targetPos
			: phone.GlobalPosition.Lerp(targetPos, posFactor);

		// Clamp in screen space so the phone always stays near the corner
		if (_settings.maxScreenDrift > 0f) {
			Vector2 phoneScreen = camera.UnprojectPosition(newPos) / viewportSize;
			Vector2 screenDrift = phoneScreen - ScreenPosition;
			if (screenDrift.Length() > _settings.maxScreenDrift) {
				// Pull it back toward the target along the camera's forward axis
				Vector2 clampedScreen = ScreenPosition + screenDrift.Normalized() * _settings.maxScreenDrift;
				Vector2 clampedPixel = clampedScreen * viewportSize;
				Vector3 clampRayOrigin = camera.ProjectRayOrigin(clampedPixel);
				Vector3 clampRayDir = camera.ProjectRayNormal(clampedPixel);
				newPos = clampRayOrigin + clampRayDir * _settings.distanceFromCamera;
			}
		}

		phone.GlobalPosition = newPos;
		if (GirlMath.IsEqualApprox(phone.GlobalPosition, newPos, 0.005f) && !reachedPosition) {
			EmitSignal(SignalName.ReachedPosition);
			reachedPosition = true;
		}

		// Smooth rotation (clamp factor to prevent overshoot)
		float rotFactor = Mathf.Min(_settings.rotationSpeed * delta, 1f);
		Quaternion currentGlobalRot = phone.GlobalTransform.Basis.GetRotationQuaternion();
		Quaternion newGlobalRot = currentGlobalRot.Slerp(targetRot, rotFactor);
		phone.GlobalBasis = new Basis(newGlobalRot);

		if (GirlMath.IsEqualApprox(currentGlobalRot, targetRot, 0.005f) && !reachedRotation) {
			EmitSignal(SignalName.ReachedRotation);
			reachedRotation = true;
		}
	}
}
