using Godot;
using Global;


[Tool]
public partial class TerrainChunk : MeshInstance3D {

	[ExportGroup("Terrain Parameters")]
	public int NoiseRows = 181;
	public int NoiseColumns = 181;
	[Export(PropertyHint.Range, "0,1,or_greater")]
	public float NoiseScale = 0.35F;
	[Export]
	public float CellWidth = 4F;
	[Export]
	public float HeightLimit = 100F;
	[Export]
	public int NoiseSeed = 0;
	[Export]
	public FastNoiseLite noise;
	[Export]
	public Curve HeightMask;
	[Export]
	public Gradient ColorMask;
	public NoiseMapGenerator NMG;

	// specific to chunk
	private float[,] noiseMap;
	private Vector2 chunkCoordinate;
	private Vector2 offset;
	public int lodIndex = 0;
	private int[] lodStepsSizes = new int[]{1, 2, 4, 8, 18, 30};
	public bool isVisible = false;
	private bool isUpdatePending = false;


	public override void _Ready() {
		if (GetParent() is null) {
			NMG = new NoiseMapGenerator(noise);
		}
		onReload();
	}


    public override void _Process(double delta) {
        if (isUpdatePending) {
			isUpdatePending = false;
			onReload();
		}
    }


	private void onReload() {
		updateParameters();
		regenerateNoiseMap();
		generateTerrain();
		generateTexture();
		this.Visible = isVisible;
	}


	public void setTerrainParameters(TerrainParameters terrainParameters) {
		this.NoiseRows = terrainParameters.NoiseRows;
		this.NoiseColumns = terrainParameters.NoiseColumns;
		this.NoiseScale = terrainParameters.NoiseScale;
		this.CellWidth = terrainParameters.CellWidth;
		this.HeightLimit = terrainParameters.HeightLimit;
		this.noise = terrainParameters.noise;
		this.HeightMask = terrainParameters.HeightMask;
		this.ColorMask = terrainParameters.ColorMask;
		this.NMG = terrainParameters.NMG;
	}


    public void setChunkParameters(Vector2 chunkCoor, int lodIndex) {
        this.chunkCoordinate = chunkCoor;
		this.lodIndex = lodIndex;
    }


	private void regenerateNoiseMap() {
		noiseMap = NMG.Generate2DNoiseMap(NoiseRows, NoiseColumns, NoiseScale);
	}


	private void updateParameters() {
		NMG.HeightMask = this.HeightMask;
		// for lod calcs, both are same from this point on
		NoiseColumns = NoiseRows;
		lodIndex = Mathf.Min(lodIndex, lodStepsSizes.Length-1);
	}


	private Vector3 calculateSurfaceNormal(Vector3 a, Vector3 b, Vector3 c) {
		return (a-b).Cross(c-b);
	}


    private void generateTerrain() {

		int lodStepsSize = lodStepsSizes[lodIndex];
		int pointsOnLine = (NoiseRows-1)/lodStepsSize + 1;
		int totalPointsOnGrid = pointsOnLine*pointsOnLine;
		Vector3[] vertices = new Vector3[totalPointsOnGrid];
		int vertIndex = 0;

		Vector2[] uvs = new Vector2[totalPointsOnGrid];
		int uvIndex = 0;

		Vector3[] normals = new Vector3[totalPointsOnGrid];

		int[] indices = new int[6*(pointsOnLine-1)*(pointsOnLine-1)];
		int indiceIndex = 0;

		// adding vertices and their respective uvs
		for (int x = 0; x < NoiseRows; x += lodStepsSize) {
			for (int z = 0; z < NoiseColumns; z += lodStepsSize) {
				vertices[vertIndex++] = new Vector3(x*CellWidth, noiseMap[x,z]*HeightLimit, z*CellWidth);
				uvs[uvIndex++] = new Vector2(((float)x)/NoiseRows, ((float)z)/NoiseColumns);
			}
		}

		// indices and vertex normals
		Vector3 triSurfaceNormal;
		int a, b, c, d;
		for (int i = 0; i < pointsOnLine-1; i++) {
			for (int j = 0; j < pointsOnLine-1; j++) {
				a = i + j*pointsOnLine;
				b = i + (j+1)*pointsOnLine;
				c = i+1 + (j+1)*pointsOnLine;
				d = i+1 + j*pointsOnLine;
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

		// // edge vertices' normals
		// Vector3[] top = new Vector3[pointsOnLine];
		// Vector3[] bottom = new Vector3[pointsOnLine];
		// Vector3[] left = new Vector3[pointsOnLine];
		// Vector3[] right = new Vector3[pointsOnLine];

		// for (int j = 0; j < NoiseRows; j += lodStepsSize) {
		// 	top[j/lodStepsSize] = new Vector3(j*CellWidth, NMG.GetNoiseAt(-lodStepsSize, j, NoiseScale)*HeightLimit, -lodStepsSize*CellWidth);
		// 	bottom[j/lodStepsSize] = new Vector3(j*CellWidth, NMG.GetNoiseAt(NoiseColumns-1+lodStepsSize, j, NoiseScale)*HeightLimit, (NoiseColumns-1+lodStepsSize)*CellWidth);
		// }
		// for (int i = 0; i < NoiseColumns; i += lodStepsSize) {
		// 	left[i/lodStepsSize] = new Vector3(-lodStepsSize*CellWidth, NMG.GetNoiseAt(i, -lodStepsSize, NoiseScale)*HeightLimit, i*CellWidth);
		// 	right[i/lodStepsSize] = new Vector3((NoiseRows-1+lodStepsSize)*CellWidth, NMG.GetNoiseAt(i, NoiseRows-1+lodStepsSize, NoiseScale), i*CellWidth);
		// }

		// // top right and bottom left can be ignored since both won't be used
		// Vector3 topLeft, bottomRight;
		// topLeft = new Vector3(-lodStepsSize*CellWidth, NMG.GetNoiseAt(-1, -1, NoiseScale)*HeightLimit, -lodStepsSize*CellWidth);
		// bottomRight = new Vector3((NoiseColumns-1+lodStepsSize)*CellWidth, NMG.GetNoiseAt(NoiseColumns, NoiseRows, NoiseScale)*HeightLimit, (NoiseRows-1+lodStepsSize)*CellWidth);

		// // top and bottom
		// for (int j = 0; j < NoiseRows-1; j += lodStepsSize) {
		// 	triSurfaceNormal = calculateSurfaceNormal(top[j/lodStepsSize], vertices[j*pointsOnLine], vertices[(j+1)*pointsOnLine]);
		// 	normals[j*pointsOnLine] += triSurfaceNormal;
		// 	normals[(j+1)*pointsOnLine] += triSurfaceNormal;
		// 	triSurfaceNormal = calculateSurfaceNormal(top[j/lodStepsSize], vertices[(j+1)*pointsOnLine], top[(j+1)/lodStepsSize]);
		// 	normals[(j+1)*pointsOnLine] += triSurfaceNormal;

		// 	triSurfaceNormal = calculateSurfaceNormal(vertices[pointsOnLine-1 + j*pointsOnLine], bottom[(j+1)/lodStepsSize], vertices[pointsOnLine-1 + (j+1)*pointsOnLine]);
		// 	normals[pointsOnLine-1 + j*pointsOnLine] += triSurfaceNormal;
		// 	normals[pointsOnLine-1 + (j+1)*pointsOnLine] += triSurfaceNormal;
		// 	triSurfaceNormal = calculateSurfaceNormal(vertices[pointsOnLine-1 + j*pointsOnLine], bottom[j/lodStepsSize], bottom[(j+1)/lodStepsSize]);
		// 	normals[pointsOnLine-1 + j*pointsOnLine] += triSurfaceNormal;
		// }

		// // left and right
		// for (int i = 0; i < NoiseColumns-1; i += lodStepsSize) {
		// 	triSurfaceNormal = calculateSurfaceNormal(left[i/lodStepsSize], vertices[i+1], vertices[i]);
		// 	normals[i+1] += triSurfaceNormal;
		// 	normals[i] += triSurfaceNormal;
		// 	triSurfaceNormal = calculateSurfaceNormal(left[i/lodStepsSize], left[i+1], vertices[i+1]);
		// 	normals[i+1] += triSurfaceNormal;

		// 	triSurfaceNormal = calculateSurfaceNormal(vertices[i + (NoiseRows-1)*pointsOnLine], vertices[i+1 + (NoiseRows-1)*pointsOnLine], right[(i+1)/lodStepsSize]);
		// 	normals[i + (NoiseRows-1)*pointsOnLine] += triSurfaceNormal;
		// 	normals[i+1 + (NoiseRows-1)*pointsOnLine] += triSurfaceNormal;
		// 	triSurfaceNormal = calculateSurfaceNormal(right[i/lodStepsSize], vertices[i+1 + (NoiseRows-1)*pointsOnLine], right[(i+1)/lodStepsSize]);
		// 	normals[i+1 + (NoiseRows-1)*pointsOnLine] += triSurfaceNormal;
		// }

		// normals[0] += calculateSurfaceNormal(topLeft, vertices[0], top[0]);
		// normals[0] += calculateSurfaceNormal(topLeft, left[0], vertices[0]);
		// normals[totalPointsOnGrid-1] += calculateSurfaceNormal(vertices[totalPointsOnGrid-1], bottomRight, right[pointsOnLine-1]);
		// normals[totalPointsOnGrid-1] += calculateSurfaceNormal(vertices[totalPointsOnGrid-1], bottom[pointsOnLine-1], bottomRight);
		// normals[pointsOnLine-1] += calculateSurfaceNormal(vertices[pointsOnLine-1], left[pointsOnLine-1], bottom[0]);
		// normals[(pointsOnLine-1)*pointsOnLine] += calculateSurfaceNormal(top[pointsOnLine-1], vertices[(pointsOnLine-1)*pointsOnLine], right[0]);

		// normalize all vertex normals
		for (int i = 0; i < pointsOnLine; i += lodStepsSize) {
			for (int j = 0; j < pointsOnLine; j += lodStepsSize) {
				normals[i + j*pointsOnLine] = normals[i + j*pointsOnLine].Normalized();
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