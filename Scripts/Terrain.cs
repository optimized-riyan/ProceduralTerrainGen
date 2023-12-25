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
	private bool Update = false;
	[Export]
	private float CellWidth = 1F;
	[Export]
	private float HeightLimit = 10F;


	public override void _Ready() {
		generateTerrain();
	}


    public override void _Process(double delta) {
        if (Update) {
			generateTerrain();
			Update = false;
		}
    }


    private void generateTerrain() {
		NoiseMapGenerator Noise = new NoiseMapGenerator(FastNoiseLite.NoiseTypeEnum.SimplexSmooth, 0F, 1F);
		float [,] noiseMap = Noise.Generate2DNoiseMap(NoiseRows, NoiseColumns, NoiseScale);

		Vector3[] vertices = new Vector3[NoiseRows*NoiseColumns];
		int vertIndex = 0;

		Vector2[] uvs = new Vector2[NoiseRows*NoiseColumns];
		int uvIndex = 0;

		// Vector3[] normals = new Vector3[(NoiseRows-1)*(NoiseColumns-1)*6];
		// int normalIndex = 0;

		int[] indices = new int[(NoiseRows-1)*(NoiseColumns-1)*6];
		int indiceIndex = 0;

		// adding vertices and their respective uvs
		for (int x = 0; x < NoiseRows; x++) {
			for (int z = 0; z < NoiseColumns; z++) {
				vertices[vertIndex++] = new Vector3(x*CellWidth, noiseMap[x,z]*HeightLimit, z*CellWidth);
				uvs[uvIndex++] = new Vector2(((float)x)/NoiseRows, ((float)z)/NoiseColumns);
			}
		}

		for (int i = 0; i < NoiseColumns-1; i++) {
			for (int j = 0; j < NoiseRows-1; j++) {
				indices[indiceIndex++] = i + j*NoiseColumns;
				indices[indiceIndex++] = i+1 + (j+1)*NoiseColumns;
				indices[indiceIndex++] = i+1 + j*NoiseColumns;

				indices[indiceIndex++] = i + j*NoiseColumns;
				indices[indiceIndex++] = i + (j+1)*NoiseColumns;
				indices[indiceIndex++] = i+1 + (j+1)*NoiseColumns;
			}
		}

		ArrayMesh arrayMesh = new ArrayMesh();
		Godot.Collections.Array array = new Godot.Collections.Array();
		array.Resize((int) Mesh.ArrayType.Max);
		array[(int)Mesh.ArrayType.Vertex] = vertices;
		array[(int)Mesh.ArrayType.TexUV] = uvs;
		array[(int)Mesh.ArrayType.Index] = indices;

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

		this.Mesh = arrayMesh;
	}
}