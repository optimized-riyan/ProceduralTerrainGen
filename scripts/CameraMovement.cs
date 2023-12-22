using System;
using Godot;

public partial class CameraMovement : Camera3D
{

	const float MOUSE_SENSITIVITY = 0.1F;
	const float MOVEMENT_SPEED = 2F;
	

	Vector3 dir = new Vector3(0, 0, 0);


	public override void _Ready() {
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}


    public override void _PhysicsProcess(double delta) {
        handleMovement(delta);
    }


	private void handleMovement(double delta) {

		float deltaF = (float) delta;

		dir = Vector3.Zero;

		if (Input.IsActionPressed("forward")) {
			dir += -this.GlobalTransform.Basis.Z;
		}
		if (Input.IsActionPressed("backward")) {
			dir += this.GlobalTransform.Basis.Z;
		}
		if (Input.IsActionPressed("left")) {
			dir += -this.GlobalTransform.Basis.X;
		}
		if (Input.IsActionPressed("right")) {
			dir += this.GlobalTransform.Basis.X;
		}

		dir = dir.Normalized();
		this.Position += dir * MOVEMENT_SPEED * deltaF;
	}


    public override void _Input(InputEvent @event) {
		if (@event is InputEventMouseMotion eventMouseMotion) {
			float dir_x = Mathf.DegToRad(eventMouseMotion.Relative.Y * MOUSE_SENSITIVITY * -1);
			float dir_y = Mathf.DegToRad(eventMouseMotion.Relative.X * MOUSE_SENSITIVITY * -1);

			this.RotateX(dir_x);
			
			this.RotateY(dir_y);
		}
	}
}