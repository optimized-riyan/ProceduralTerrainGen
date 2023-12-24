using Godot;
using Global;


[Tool]
public partial class Terrain : MeshInstance3D
{

	[Export]
	private int NoiseRows = 10;
	[Export]
	private int NoiseColumns = 10;
	[Export]
	private float NoiseScale = 0.1F;
	[Export]
	private bool AutoUpdate = false;
	[Export]
	private float CellWidth = 1F;
	[Export]
	private float HeightLimit = 10F;


	public override void _Ready() {
		generateTerrain();
	}


	private void generateTerrain() {
		NoiseMapGenerator Noise = new NoiseMapGenerator(FastNoiseLite.NoiseTypeEnum.SimplexSmooth, 0F, 1F);
		float [,] noiseMap = Noise.Generate2DNoiseMap(NoiseRows, NoiseColumns, NoiseScale);

		Vector3[] vertices = new Vector3[(NoiseRows-1)*(NoiseColumns-1)*6];
		int vertIndex = 0;
		float lengthB2 = NoiseRows*CellWidth/2;
		float widthB2 = NoiseColumns*CellWidth/2;

		Vector2[] uvs = new Vector2[(NoiseRows-1)*(NoiseColumns-1)*6];
		int uvIndex = 0;

		Vector3[] normals = new Vector3[(NoiseRows-1)*(NoiseColumns-1)*6];
		int normalIndex = 0;

		for (int x = 0; x < NoiseRows-1; x++) {
			for (int z = 0; z < NoiseColumns-1; z++) {
				// triangle 1
				vertices[vertIndex++] = new Vector3(x*CellWidth-lengthB2, noiseMap[x,z]*HeightLimit, z*CellWidth-widthB2);
				vertices[vertIndex++] = new Vector3((x+1)*CellWidth-lengthB2, noiseMap[x+1,z+1]*HeightLimit, (z+1)*CellWidth-widthB2);
				vertices[vertIndex++] = new Vector3(x*CellWidth-lengthB2, noiseMap[x,z+1]*HeightLimit, (z+1)*CellWidth-widthB2);

				// triangle 2
				vertices[vertIndex++] = new Vector3(x*CellWidth-lengthB2, noiseMap[x,z]*HeightLimit, z*CellWidth-widthB2);
				vertices[vertIndex++] = new Vector3((x+1)*CellWidth-lengthB2, noiseMap[x+1,z]*HeightLimit, z*CellWidth-widthB2);
				vertices[vertIndex++] = new Vector3((x+1)*CellWidth-lengthB2, noiseMap[x+1,z+1]*HeightLimit, (z+1)*CellWidth-widthB2);

				// uv of tri 1, using z as y
				uvs[uvIndex++] = new Vector2(x/NoiseRows, z/NoiseColumns);
				uvs[uvIndex++] = new Vector2((x+1)/NoiseRows, (z+1)/NoiseColumns);
				uvs[uvIndex++] = new Vector2(x/NoiseRows, (z+1)/NoiseColumns);

				// uv of tri 2, again using z as y
				uvs[uvIndex++] = new Vector2(x/NoiseRows, z/NoiseColumns);
				uvs[uvIndex++] = new Vector2((x+1)/NoiseRows, z/NoiseColumns);
				uvs[uvIndex++] = new Vector2((x+1)/NoiseRows, (z+1)/NoiseColumns);
			}
		}

		ArrayMesh arrayMesh = new ArrayMesh();
		Godot.Collections.Array array = new Godot.Collections.Array();
		array.Resize((int) Mesh.ArrayType.Max);
		array[(int)Mesh.ArrayType.Vertex] = vertices;
		array[(int)Mesh.ArrayType.TexUV] = uvs;

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

		this.Mesh = arrayMesh;
	}
}