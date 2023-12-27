using Godot;
using Global;


[Tool]
public partial class Terrain : MeshInstance3D
{

	[ExportGroup("Terrain Parameters")]
	[Export(PropertyHint.Range, "2,256,")]
	private int NoiseRows = 128;
	[Export(PropertyHint.Range, "2,256,")]
	private int NoiseColumns = 128;
	[Export(PropertyHint.Range, "0,1,or_greater")]
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
	private Gradient ColorMask;
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


	private Vector3 calculateSurfaceNormal(Vector3 a, Vector3 b, Vector3 c) {
		return (a-b).Cross(c-b);
	}


    private void generateTerrain() {

		Vector3[] vertices = new Vector3[NoiseRows*NoiseColumns];
		int vertIndex = 0;

		Vector2[] uvs = new Vector2[NoiseRows*NoiseColumns];
		int uvIndex = 0;

		Vector3[] normals = new Vector3[NoiseRows*NoiseColumns];

		int[] indices = new int[(NoiseRows-1)*(NoiseColumns-1)*6];
		int indiceIndex = 0;

		// adding vertices and their respective uvs
		for (int x = 0; x < NoiseRows; x++) {
			for (int z = 0; z < NoiseColumns; z++) {
				vertices[vertIndex++] = new Vector3(x*CellWidth, noiseMap[x,z]*HeightLimit, z*CellWidth);
				uvs[uvIndex++] = new Vector2(((float)x)/NoiseRows, ((float)z)/NoiseColumns);
			}
		}

		// indices and vertex normals
		Vector3 triSurfaceNormal;
		int a, b, c, d;
		for (int i = 0; i < NoiseColumns-1; i++) {
			for (int j = 0; j < NoiseRows-1; j++) {
				a = i + j*NoiseColumns;
				b = i + (j+1)*NoiseColumns;
				c = i+1 + (j+1)*NoiseColumns;
				d = i+1 + j*NoiseColumns;
				indices[indiceIndex++] = a;
				indices[indiceIndex++] = c;
				indices[indiceIndex++] = d;

				indices[indiceIndex++] = a;
				indices[indiceIndex++] = b;
				indices[indiceIndex++] = c;

				triSurfaceNormal = calculateSurfaceNormal(vertices[a], vertices[c], vertices[d]);
				normals[a] += triSurfaceNormal;
				normals[c] += triSurfaceNormal;
				normals[d] += triSurfaceNormal;

				triSurfaceNormal = calculateSurfaceNormal(vertices[a], vertices[b], vertices[c]);
				normals[a] += triSurfaceNormal;
				normals[b] += triSurfaceNormal;
				normals[c] += triSurfaceNormal;
			}
		}

		// normalize all vertex normals
		for (int i = 0; i < NoiseColumns; i++) {
			for (int j = 0; j < NoiseRows; j++) {
				normals[i + j*NoiseColumns] = normals[i + j*NoiseColumns].Normalized();
			}
		}

		// edge vertices normals
		float[] top = new float[NoiseRows];
		float[] bottom = new float[NoiseRows];
		float[] left = new float[NoiseColumns];
		float[] right = new float[NoiseColumns];

		for (int j = 0; j < NoiseRows; j++) {
			top[j] = NMG.GetNoiseAt(0, j, NoiseScale);
			bottom[j] = NMG.GetNoiseAt(NoiseColumns-1, j, NoiseScale);
		}
		for (int i = 0; i < NoiseColumns; i++) {
			left[i] = NMG.GetNoiseAt(i, 0, NoiseScale);
			right[i] = NMG.GetNoiseAt(i, NoiseRows-1, NoiseScale);
		}


		ArrayMesh arrayMesh = new ArrayMesh();
		Godot.Collections.Array array = new Godot.Collections.Array();
		array.Resize((int) Mesh.ArrayType.Max);
		array[(int)Mesh.ArrayType.Vertex] = vertices;
		array[(int)Mesh.ArrayType.TexUV] = uvs;
		array[(int)Mesh.ArrayType.Index] = indices;
		array[(int)Mesh.ArrayType.Normal] = normals;

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

		this.Mesh = arrayMesh;
	}


	private void generateTexture() {
		Image img = new Image();
		byte[] imageData = new byte[NoiseRows*NoiseColumns*3];
		Color sampledPoint;

		for (int i = 0; i < NoiseRows; i++) {
			for (int j = 0; j < NoiseColumns; j++) {
				sampledPoint = ColorMask.Sample(noiseMap[i,j]);
				imageData[(i + j*NoiseRows)*3] = (byte)sampledPoint.R8;
				imageData[(i + j*NoiseRows)*3 + 1] = (byte)sampledPoint.G8;
				imageData[(i + j*NoiseRows)*3 + 2] = (byte)sampledPoint.B8;
			}
		}
		img.SetData(NoiseRows, NoiseColumns, false, Image.Format.Rgb8, imageData);
		
		Texture2D texture = ImageTexture.CreateFromImage(img);
        StandardMaterial3D material = new StandardMaterial3D { 
			AlbedoTexture = texture,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest
		};
        this.MaterialOverride = material;
	}
}