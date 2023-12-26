using Godot;
using Global;


[Tool]
public partial class Terrain : MeshInstance3D
{

	[ExportGroup("Terrain Parameters")]
	[Export]
	private int NoiseRows = 128;
	[Export]
	private int NoiseColumns = 128;
	[Export]
	private float NoiseScale = 0.35F;
	[Export]
	private float CellWidth = 4F;
	[Export]
	private float HeightLimit = 100F;
	[Export]
	private int NoiseSeed = 0;
	[Export]
	private Curve HeightMask;
	[Export]
	private bool Update = false;

	private NoiseMapGenerator NMG = new NoiseMapGenerator(FastNoiseLite.NoiseTypeEnum.Perlin, 0F, 1F);
	private float[,] noiseMap;


	public override void _Ready() {
		// setting specific noise parameters
		NMG.noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
		NMG.noise.Seed = NoiseSeed;
		NMG.noise.FractalGain = 0.5F;
		NMG.noise.FractalLacunarity = 2F;
		NMG.noise.FractalOctaves = 4;
		NMG.noise.Frequency = 0.01F;

		updateParameters();
		regenerateNoiseMap();
		generateTerrain();
		generateTexture();
	}


    public override void _Process(double delta) {
        if (Update) {
			Update = false;
			updateParameters();
			regenerateNoiseMap();
			generateTerrain();
			generateTexture();
		}
    }


	private void regenerateNoiseMap() {
		noiseMap = NMG.Generate2DNoiseMap(NoiseRows, NoiseColumns, NoiseScale);
	}


	private void updateParameters() {
		NMG.HeightMask = this.HeightMask;
	}


    private void generateTerrain() {

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


	private void generateTexture() {
		Image img = new Image();
		byte[] imageData = new byte[NoiseRows*NoiseColumns];

		for (int i = 0; i < NoiseRows; i++) {
			for (int j = 0; j < NoiseColumns; j++) {
				imageData[i + j*NoiseRows] = (byte)Mathf.Lerp(0, 255, noiseMap[i,j]);
			}
		}
		img.SetData(NoiseRows, NoiseColumns, false, Image.Format.L8, imageData);
		
		Texture2D texture = ImageTexture.CreateFromImage(img);
		StandardMaterial3D material = new StandardMaterial3D();
		material.AlbedoTexture = texture;
		this.MaterialOverride = material;
	}
}