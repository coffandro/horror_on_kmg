using Godot;

public partial class Teleporter : Area3D {
	[Export] public Teleporter target;

	[Export] public bool targetX = true;
	[Export] public bool targetY = false;
	[Export] public bool targetZ = true;

	public bool recievedPlayer;

	public void _on_body_entered(Node3D body) {
		if (body is Player player && !recievedPlayer && target != null) {
			Vector3 pos = player.GlobalPosition;
			Vector3 offsetFromThisTeleporter = player.GlobalPosition - GlobalPosition;

			if (targetX) { pos.X = target.GlobalPosition.X + offsetFromThisTeleporter.X; }
			if (targetY) { pos.Y = target.GlobalPosition.Y + offsetFromThisTeleporter.Y; }
			if (targetZ) { pos.Z = target.GlobalPosition.Z + offsetFromThisTeleporter.Z; }

			player.TeleportTo(pos);

			target.recievedPlayer = true;
		}
	}

	public void _on_body_exited(Node3D body) {
		if (body is Player) {
			recievedPlayer = false;
		}
	}
}
