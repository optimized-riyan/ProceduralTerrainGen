using Godot;
using Global;


public partial class Terrain : MeshInstance3D
{
	public override void _Ready() {
		generateTerrain();
	}

	private void generateTerrain() {
		NoiseMapGenerator NMG = new NoiseMapGenerator(FastNoiseLite.NoiseTypeEnum.Perlin);

		Vector3[] vertices = new Vector3[3];
		vertices[0] = new Vector3(0, 1, 0);
		vertices[1] = new Vector3(1, 0, 0);
		vertices[2] = new Vector3(0, 0, 1);

		ArrayMesh arrayMesh = new ArrayMesh();
		Godot.Collections.Array array = new Godot.Collections.Array();
		array.Resize((int) Mesh.ArrayType.Max);
		array[(int)Mesh.ArrayType.Vertex] = vertices;

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

		this.Mesh = arrayMesh;
	}
}