using Godot;

public partial class Suzanne : Node3D {
	[Export] public Node3D target;
	[Export] public float speed;

	public override void _PhysicsProcess(double delta) {
		base._PhysicsProcess(delta);

		target.RotateY(
			Mathf.DegToRad(target.RotationDegrees.Y + (speed * (float)delta))
		);
	}
}
