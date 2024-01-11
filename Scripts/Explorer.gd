extends CharacterBody3D


const mouse_sensitity = .1
var speed = 35
var sprint_multiplier = 1.5 - 1
const gravity = -50
const jump = 20
var jump_count = 20000000
const accel = 10
const air_accel = 2
var jumps_left = jump_count

@onready var head: Node3D = $Head


var dir: Vector3
var current_velocity: Vector3 = Vector3.ZERO


func _ready():
	up_direction = Vector3.UP
	floor_max_angle = deg_to_rad(75)


func _physics_process(delta):
	apply_gravity(delta)
	apply_jump()
	apply_movement()


func apply_gravity(delta):
	if not is_on_floor():
		velocity.y += gravity * delta


func apply_jump():
	if is_on_floor():
		jumps_left = jump_count
	
	if Input.is_action_just_pressed("jump"):
		if jumps_left > 0:
			velocity.y = jump
			jumps_left -= 1


func apply_movement():
	dir = Vector3.ZERO
	
	if Input.is_action_pressed("forward"):
		dir -= self.global_transform.basis.z
	if Input.is_action_pressed("backward"):
		dir += self.global_transform.basis.z
	if Input.is_action_pressed("left"):
		dir -= self.global_transform.basis.x
	if Input.is_action_pressed("right"):
		dir += self.global_transform.basis.x
	
	dir = dir.normalized()
	
	
	var target_velocity = dir * speed * (1 + sprint_multiplier * (1 if Input.is_action_pressed("sprint") else 0))
	
	current_velocity = current_velocity.move_toward(target_velocity, accel)
	
	velocity.x = current_velocity.x
	velocity.z = current_velocity.z
	
	move_and_slide()


# Mouse inputs
func _input(event):
	if event is InputEventMouseMotion:
		var dir_x = deg_to_rad(event.relative.y * mouse_sensitity * -1)
		var dir_y = deg_to_rad(event.relative.x * mouse_sensitity * -1)
		
		# Rotates the view vertically
		head.rotate_x(dir_x)
		head.rotation_degrees.x = clamp(head.rotation_degrees.x, -90, 90)
		
		# Rotates the view horizontally
		self.rotate_y(dir_y)
