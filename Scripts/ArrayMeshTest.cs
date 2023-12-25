using Godot;


[Tool]
public partial class ArrayMeshTest : MeshInstance3D
{
	float edgeLength = 10F;
	float height = 10F;


	public override void _Ready() {
		Vector3[] vertices = new Vector3[6];
		int[] indices = new int[12];

		vertices[0] = new Vector3(0, height, 0);
		vertices[1] = new Vector3(0, height, edgeLength);
		vertices[2] = new Vector3(0, height, 2*edgeLength);
		vertices[3] = new Vector3(edgeLength, height, 0);
		vertices[4] = new Vector3(edgeLength, height, edgeLength);
		vertices[5] = new Vector3(edgeLength, height, 2*edgeLength);

		indices[0] = 0;
		indices[1] = 4;
		indices[2] = 1;
		indices[3] = 0;
		indices[4] = 3;
		indices[5] = 4;
		indices[6] = 1;
		indices[7] = 5;
		indices[8] = 2;
		indices[9] = 1;
		indices[10] = 4;
		indices[11] = 5;

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		ArrayMesh arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

		this.Mesh = arrayMesh;
	}
}