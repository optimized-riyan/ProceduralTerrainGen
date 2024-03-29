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


    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("ui_cancel")) {
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
				Input.MouseMode = Input.MouseModeEnum.Visible;
			else if (Input.MouseMode == Input.MouseModeEnum.Visible)
				Input.MouseMode = Input.MouseModeEnum.Captured;
		}
    }


    public override void _PhysicsProcess(double delta) {
        handleMovement(delta);
    }


    public override void _Input(InputEvent @event) {
		if (@event is InputEventMouseMotion eventMouseMotion) {
			float dir_x = Mathf.DegToRad(eventMouseMotion.Relative.Y * MOUSE_SENSITIVITY * -1);
			float dir_y = Mathf.DegToRad(eventMouseMotion.Relative.X * MOUSE_SENSITIVITY * -1);

			this.RotateX(dir_x);
			float clampedX = Mathf.DegToRad(Mathf.Clamp(this.RotationDegrees.X, -90, 90));
			this.Rotation = new Vector3(clampedX, this.Rotation.Y, this.Rotation.Z);
			
			this.RotateY(dir_y);
		}
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
}