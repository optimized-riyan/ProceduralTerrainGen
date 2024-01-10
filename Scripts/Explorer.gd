extends CharacterBody3D


const MOUSE_SENSITIVITY = .1
const SPEED = 10
const SPRINT_SPEED = 15
const GRAVITY = -50
const JUMP = 20
const JUMP_COUNT = 2
const ACCEL = 10
const AIR_ACCEL = 2

@onready var head: Node3D = $Head


var dir: Vector3
var jump_count: int = JUMP_COUNT
var current_velocity: Vector3 = Vector3.ZERO
var mouse_dir: Vector2
var is_adsing: bool = false
var current_mouse_sensitivity: float


func _ready():
	up_direction = Vector3.UP
	floor_max_angle = deg_to_rad(75)


func _physics_process(delta):
	apply_gravity(delta)
	apply_jump()
	apply_movement()


func apply_gravity(delta):
	if not is_on_floor():
		velocity.y += GRAVITY * delta


func apply_jump():
	if is_on_floor():
		jump_count = JUMP_COUNT
	
	if Input.is_action_just_pressed("jump"):
		if jump_count > 0:
			velocity.y = JUMP
			jump_count -= 1


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
	
	
	var accel = ACCEL if is_on_floor() else AIR_ACCEL
	var speed = SPRINT_SPEED if Input.is_action_pressed("sprint") else SPEED
	
	var target_velocity = dir * speed
	
	current_velocity = current_velocity.move_toward(target_velocity, accel)
	
	velocity.x = current_velocity.x
	velocity.z = current_velocity.z
	
	move_and_slide()


# Mouse inputs
func _input(event):
	if event is InputEventMouseMotion:
		var dir_x = deg_to_rad(event.relative.y * MOUSE_SENSITIVITY * -1)
		var dir_y = deg_to_rad(event.relative.x * MOUSE_SENSITIVITY * -1)
		
		# Rotates the view vertically
		head.rotate_x(dir_x)
		head.rotation_degrees.x = clamp(head.rotation_degrees.x, -90, 90)
		
		# Rotates the view horizontally
		self.rotate_y(dir_y)
