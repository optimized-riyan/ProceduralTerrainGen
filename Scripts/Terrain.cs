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
	private float HeightLimit = 20F;


	public override void _Ready() {
		generateTerrain();
	}


	private void generateTerrain() {
		NoiseMapGenerator Noise = new NoiseMapGenerator(FastNoiseLite.NoiseTypeEnum.Perlin, 0F, 1F);
		float [,] noiseMap = Noise.Generate2DNoiseMap(NoiseRows, NoiseColumns, NoiseScale);

		Vector3[] vertices = new Vector3[(NoiseRows-1)*(NoiseColumns-1)*6];
		int currentIndex = 0;
		for (int x = 0; x < NoiseRows-1; x++) {
			for (int z = 0; z < NoiseColumns-1; z++) {
				vertices[currentIndex++] = new Vector3(x*CellWidth, noiseMap[x,z]*HeightLimit, z*CellWidth);
				vertices[currentIndex++] = new Vector3((x+1)*CellWidth, noiseMap[x+1,z+1]*HeightLimit, (z+1)*CellWidth);
				vertices[currentIndex++] = new Vector3(x*CellWidth, noiseMap[x,z+1]*HeightLimit, (z+1)*CellWidth);

				vertices[currentIndex++] = new Vector3(x*CellWidth, noiseMap[x,z]*HeightLimit, z*CellWidth);
				vertices[currentIndex++] = new Vector3((x+1)*CellWidth, noiseMap[x+1,z]*HeightLimit, z*CellWidth);
				vertices[currentIndex++] = new Vector3((x+1)*CellWidth, noiseMap[x+1,z+1]*HeightLimit, (z+1)*CellWidth);
			}
		}

		ArrayMesh arrayMesh = new ArrayMesh();
		Godot.Collections.Array array = new Godot.Collections.Array();
		array.Resize((int) Mesh.ArrayType.Max);
		array[(int)Mesh.ArrayType.Vertex] = vertices;

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

		this.Mesh = arrayMesh;
	}
}