extends MeshInstance3D


# Called when the node enters the scene tree for the first time.
func _ready():
	var vertices = PackedVector3Array()
	vertices.push_back(Vector3(0, 1, 0))
	vertices.push_back(Vector3(1, 0, 0))
	vertices.push_back(Vector3(0, 0, 1))

	# Initialize the ArrayMesh.
	var arr_mesh = ArrayMesh.new()
	var arrays = []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = vertices

	# Create the Mesh.
	arr_mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	var m = MeshInstance3D.new()
	m.mesh = arr_mesh
	get_parent().add_child.call_deferred(m)
