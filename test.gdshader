shader_type spatial;

void vertex() {
	VERTEX.x += .2*sin(TIME);
}

void fragment() {
	
}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
