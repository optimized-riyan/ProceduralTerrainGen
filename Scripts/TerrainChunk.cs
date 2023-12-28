using Godot;
using Global;


[Tool]
public partial class TerrainChunk : MeshInstance3D {

	[ExportGroup("Terrain Parameters")]
	[Export(PropertyHint.Range, "2,256,")]
	private int NoiseRows = 181;
	[Export(PropertyHint.Range, "2,256,")]
	private int NoiseColumns = 181;
	[Export(PropertyHint.Range, "0,1,or_greater")]
	private float NoiseScale = 0.35F;
	[Export]
	private float CellWidth = 4F;
	[Export]
	private float HeightLimit = 100F;
	[Export]
	private int NoiseSeed = 0;
	[Export]
	private FastNoiseLite noise;
	[Export]
	private Curve HeightMask;
	[Export]
	private Gradient ColorMask;
	[Export]
	private bool Update = false;

	private NoiseMapGenerator NMG;
	private float[,] noiseMap;
	private Vector2 chunkCoordinate;
	public int lodIndex;


	// public TerrainChunk(Vector2 chunkCoor) {

	// }


	public override void _Ready() {
		NMG = new NoiseMapGenerator(noise);

		onReload();
	}


    public override void _Process(double delta) {
        if (Update) {
			Update = false;
			onReload();
		}
    }


	private void onReload() {
		updateParameters();
		regenerateNoiseMap();
		generateTerrain();
		generateTexture();
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

		// edge vertices normals
		Vector3[] top = new Vector3[NoiseRows];
		Vector3[] bottom = new Vector3[NoiseRows];
		Vector3[] left = new Vector3[NoiseColumns];
		Vector3[] right = new Vector3[NoiseColumns];

		for (int j = 0; j < NoiseRows; j++) {
			top[j] = new Vector3(j*CellWidth, NMG.GetNoiseAt(-1, j, NoiseScale)*HeightLimit, -1*CellWidth);
			bottom[j] = new Vector3(j*CellWidth, NMG.GetNoiseAt(NoiseColumns, j, NoiseScale)*HeightLimit, NoiseColumns*CellWidth);
		}
		for (int i = 0; i < NoiseColumns; i++) {
			left[i] = new Vector3(-1*CellWidth, NMG.GetNoiseAt(i, -1, NoiseScale)*HeightLimit, i*CellWidth);
			right[i] = new Vector3(NoiseRows*CellWidth, NMG.GetNoiseAt(i, NoiseRows, NoiseScale), i*CellWidth);
		}

		// top right and bottom left can be ignored since both won't be used
		Vector3 topLeft, bottomRight;
		topLeft = new Vector3(-1*CellWidth, NMG.GetNoiseAt(-1, -1, NoiseScale)*HeightLimit, -1*CellWidth);
		bottomRight = new Vector3(NoiseColumns*CellWidth, NMG.GetNoiseAt(NoiseColumns, NoiseRows, NoiseScale)*HeightLimit, NoiseRows*CellWidth);

		// top and bottom
		for (int j = 0; j < NoiseRows-1; j++) {
			triSurfaceNormal = calculateSurfaceNormal(top[j], vertices[j*NoiseColumns], vertices[(j+1)*NoiseColumns]);
			normals[j*NoiseColumns] += triSurfaceNormal;
			normals[(j+1)*NoiseColumns] += triSurfaceNormal;
			triSurfaceNormal = calculateSurfaceNormal(top[j], vertices[(j+1)*NoiseColumns], top[j+1]);
			normals[(j+1)*NoiseColumns] += triSurfaceNormal;

			triSurfaceNormal = calculateSurfaceNormal(vertices[NoiseColumns-1 + j*NoiseColumns], bottom[j+1], vertices[NoiseColumns-1 + (j+1)*NoiseColumns]);
			normals[NoiseColumns-1 + j*NoiseColumns] += triSurfaceNormal;
			normals[NoiseColumns-1 + (j+1)*NoiseColumns] += triSurfaceNormal;
			triSurfaceNormal = calculateSurfaceNormal(vertices[NoiseColumns-1 + j*NoiseColumns], bottom[j], bottom[j+1]);
			normals[NoiseColumns-1 + j*NoiseColumns] += triSurfaceNormal;
		}

		// left and right
		for (int i = 0; i < NoiseColumns-1; i++) {
			triSurfaceNormal = calculateSurfaceNormal(left[i], vertices[i+1], vertices[i]);
			normals[i+1] += triSurfaceNormal;
			normals[i] += triSurfaceNormal;
			triSurfaceNormal = calculateSurfaceNormal(left[i], left[i+1], vertices[i+1]);
			normals[i+1] += triSurfaceNormal;

			triSurfaceNormal = calculateSurfaceNormal(vertices[i + (NoiseRows-1)*NoiseColumns], vertices[i+1 + (NoiseRows-1)*NoiseColumns], right[i+1]);
			normals[i + (NoiseRows-1)*NoiseColumns] += triSurfaceNormal;
			normals[i+1 + (NoiseRows-1)*NoiseColumns] += triSurfaceNormal;
			triSurfaceNormal = calculateSurfaceNormal(right[i], vertices[i+1 + (NoiseRows-1)*NoiseColumns], right[i+1]);
			normals[i+1 + (NoiseRows-1)*NoiseColumns] += triSurfaceNormal;
		}

		normals[0] += calculateSurfaceNormal(topLeft, vertices[0], top[0]);
		normals[0] += calculateSurfaceNormal(topLeft, left[0], vertices[0]);
		normals[NoiseRows*NoiseColumns-1] += calculateSurfaceNormal(vertices[NoiseRows*NoiseColumns - 1], bottomRight, right[NoiseColumns-1]);
		normals[NoiseRows*NoiseColumns-1] += calculateSurfaceNormal(vertices[NoiseRows*NoiseColumns-1], bottom[NoiseRows-1], bottomRight);
		normals[NoiseColumns-1] += calculateSurfaceNormal(vertices[NoiseColumns-1], left[NoiseColumns-1], bottom[0]);
		normals[(NoiseRows-1)*NoiseColumns] += calculateSurfaceNormal(top[NoiseRows-1], vertices[(NoiseRows-1)*NoiseColumns], right[0]);

		// normalize all vertex normals
		for (int i = 0; i < NoiseColumns; i++) {
			for (int j = 0; j < NoiseRows; j++) {
				normals[i + j*NoiseColumns] = normals[i + j*NoiseColumns].Normalized();
			}
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


	private void updateTerrainChunkLod() {

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