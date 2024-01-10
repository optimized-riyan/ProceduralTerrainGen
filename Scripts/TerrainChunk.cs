using Godot;
using Global;


[Tool]
public partial class TerrainChunk : StaticBody3D {

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
    private MeshInstance3D terrainChunkMesh;
    private CollisionShape3D collider;
    private float[,] noiseMap;
    private Material material;
    private HeightMapShape3D heightMapShape3D;
    public Vector2I chunkCoordinate;
    private Vector2I offset;
    public int lodIndex = 0;
    private int[] lodStepSizes = new int[]{ 1, 2, 4, 9, 18, 30 };
    private ArrayMesh[] cachedArrayMeshes = new ArrayMesh[6];
    private bool isColliderGenerated = false, isColliderAdded = false;


    public TerrainChunk() {
        collider = new CollisionShape3D();
    }


    public override void _Ready() {
        if (GetParent() is null) { NMG = new NoiseMapGenerator(noise); }
        terrainChunkMesh = GetNode<MeshInstance3D>("TerrainChunkMesh");
        collider.Position = new Vector3((NoiseColumns-1)*CellWidth/2, 0, (NoiseRows-1)*CellWidth/2);
    }


    public void OnNew() {
        UpdateParameters();
        GenerateNoiseMap();
        GenerateTexture();
    }


    public void SetTerrainParameters(TerrainParameters terrainParameters) {
        this.NoiseRows = terrainParameters.NoiseRows;
        this.NoiseColumns = terrainParameters.NoiseRows;
        this.NoiseScale = terrainParameters.NoiseScale;
        this.CellWidth = terrainParameters.CellWidth;
        this.HeightLimit = terrainParameters.HeightLimit;
        this.noise = terrainParameters.noise;
        this.HeightMask = terrainParameters.HeightMask;
        this.ColorMask = terrainParameters.ColorMask;
        this.NMG = terrainParameters.NMG;
    }


    public void SetChunkParameters(Vector2I chunkCoor) {
        this.chunkCoordinate = chunkCoor;
        this.offset = new Vector2I(chunkCoordinate.X*(NoiseRows-1), chunkCoordinate.Y*(NoiseColumns-1));
        this.Position = new Vector3(offset.X*CellWidth, 0, offset.Y*CellWidth);
    }


    private void GenerateNoiseMap() {
        noiseMap = NMG.Generate2DNoiseMap(NoiseRows, NoiseColumns, chunkCoordinate.X*(NoiseRows-1), chunkCoordinate.Y*(NoiseColumns-1), NoiseScale);
    }


    private void UpdateParameters() {
        NMG.HeightMask = this.HeightMask;
        NoiseColumns = NoiseRows;
        lodIndex = Mathf.Min(lodIndex, lodStepSizes.Length-1);
    }


    private Vector3 CalculateSurfaceNormal(Vector3 a, Vector3 b, Vector3 c) { return (a-b).Cross(c-b); }


    public void UpdateLOD(float lodF) {
        lodIndex = Mathf.FloorToInt(lodF*lodStepSizes.Length);
        GenerateTerrainMesh();
        if (lodIndex == 0)
            GenerateCollisionShape();
    }

    public void UpdateLOD() { GenerateTerrainMesh(); } 


    private void GenerateTerrainMesh() {

        int lodStepsSize = lodStepSizes[lodIndex];
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

                triSurfaceNormal = CalculateSurfaceNormal(vertices[a], vertices[c], vertices[d]);
                normals[a] += triSurfaceNormal;
                normals[c] += triSurfaceNormal;
                normals[d] += triSurfaceNormal;

                triSurfaceNormal = CalculateSurfaceNormal(vertices[a], vertices[b], vertices[c]);
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
        //     top[j/lodStepsSize] = new Vector3(j*CellWidth, NMG.GetNoiseAt(offset.X-lodStepsSize, offset.Y+j, NoiseScale)*HeightLimit, -lodStepsSize*CellWidth);
        //     bottom[j/lodStepsSize] = new Vector3(j*CellWidth, NMG.GetNoiseAt(offset.X+NoiseColumns-1+lodStepsSize, offset.Y+j, NoiseScale)*HeightLimit, (NoiseColumns-1+lodStepsSize)*CellWidth);
        // }
        // for (int i = 0; i < NoiseColumns; i += lodStepsSize) {
        //     left[i/lodStepsSize] = new Vector3(-lodStepsSize*CellWidth, NMG.GetNoiseAt(offset.X+i, offset.Y-lodStepsSize, NoiseScale)*HeightLimit, i*CellWidth);
        //     right[i/lodStepsSize] = new Vector3((NoiseRows-1+lodStepsSize)*CellWidth, NMG.GetNoiseAt(offset.X+i, offset.Y+NoiseRows-1+lodStepsSize, NoiseScale), i*CellWidth);
        // }

        // // top right and bottom left can be ignored since both won't be used
        // Vector3 topLeft, bottomRight;
        // topLeft = new Vector3(-lodStepsSize*CellWidth, NMG.GetNoiseAt(offset.X-1, offset.Y-1, NoiseScale)*HeightLimit, -lodStepsSize*CellWidth);
        // bottomRight = new Vector3((NoiseColumns-1+lodStepsSize)*CellWidth, NMG.GetNoiseAt(offset.X+NoiseColumns, offset.Y+NoiseRows, NoiseScale)*HeightLimit, (NoiseRows-1+lodStepsSize)*CellWidth);

        // // top and bottom
        // for (int j = 0; j < NoiseRows-1; j += lodStepsSize) {
        //     GD.Print($"{top.Length} {j/lodStepsSize} {(j+1)*pointsOnLine}");
        //     triSurfaceNormal = CalculateSurfaceNormal(top[j/lodStepsSize], vertices[j*pointsOnLine], vertices[(j+1)*pointsOnLine]);
        //     normals[j*pointsOnLine] += triSurfaceNormal;
        //     normals[(j+1)*pointsOnLine] += triSurfaceNormal;
        //     triSurfaceNormal = CalculateSurfaceNormal(top[j/lodStepsSize], vertices[(j+1)*pointsOnLine], top[(j+1)/lodStepsSize]);
        //     normals[(j+1)*pointsOnLine] += triSurfaceNormal;

        //     triSurfaceNormal = CalculateSurfaceNormal(vertices[pointsOnLine-1 + j*pointsOnLine], bottom[(j+1)/lodStepsSize], vertices[pointsOnLine-1 + (j+1)*pointsOnLine]);
        //     normals[pointsOnLine-1 + j*pointsOnLine] += triSurfaceNormal;
        //     normals[pointsOnLine-1 + (j+1)*pointsOnLine] += triSurfaceNormal;
        //     triSurfaceNormal = CalculateSurfaceNormal(vertices[pointsOnLine-1 + j*pointsOnLine], bottom[j/lodStepsSize], bottom[(j+1)/lodStepsSize]);
        //     normals[pointsOnLine-1 + j*pointsOnLine] += triSurfaceNormal;
        // }

        // // left and right
        // for (int i = 0; i < NoiseColumns-1; i += lodStepsSize) {
        //     triSurfaceNormal = CalculateSurfaceNormal(left[i/lodStepsSize], vertices[i+1], vertices[i]);
        //     normals[i+1] += triSurfaceNormal;
        //     normals[i] += triSurfaceNormal;
        //     triSurfaceNormal = CalculateSurfaceNormal(left[i/lodStepsSize], left[i+1], vertices[i+1]);
        //     normals[i+1] += triSurfaceNormal;

        //     triSurfaceNormal = CalculateSurfaceNormal(vertices[i + (NoiseRows-1)*pointsOnLine], vertices[i+1 + (NoiseRows-1)*pointsOnLine], right[(i+1)/lodStepsSize]);
        //     normals[i + (NoiseRows-1)*pointsOnLine] += triSurfaceNormal;
        //     normals[i+1 + (NoiseRows-1)*pointsOnLine] += triSurfaceNormal;
        //     triSurfaceNormal = CalculateSurfaceNormal(right[i/lodStepsSize], vertices[i+1 + (NoiseRows-1)*pointsOnLine], right[(i+1)/lodStepsSize]);
        //     normals[i+1 + (NoiseRows-1)*pointsOnLine] += triSurfaceNormal;
        // }

        // normals[0] += CalculateSurfaceNormal(topLeft, vertices[0], top[0]);
        // normals[0] += CalculateSurfaceNormal(topLeft, left[0], vertices[0]);
        // normals[totalPointsOnGrid-1] += CalculateSurfaceNormal(vertices[totalPointsOnGrid-1], bottomRight, right[pointsOnLine-1]);
        // normals[totalPointsOnGrid-1] += CalculateSurfaceNormal(vertices[totalPointsOnGrid-1], bottom[pointsOnLine-1], bottomRight);
        // normals[pointsOnLine-1] += CalculateSurfaceNormal(vertices[pointsOnLine-1], left[pointsOnLine-1], bottom[0]);
        // normals[(pointsOnLine-1)*pointsOnLine] += CalculateSurfaceNormal(top[pointsOnLine-1], vertices[(pointsOnLine-1)*pointsOnLine], right[0]);

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
        cachedArrayMeshes[lodIndex] = arrayMesh;
    }


    private void GenerateTexture() {
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
        this.material = material;
    }

    
    private void GenerateCollisionShape() {
        if (!isColliderGenerated) {
            heightMapShape3D = new HeightMapShape3D {
                MapWidth = (NoiseRows-1)/lodStepSizes[1] + 1,
                MapDepth = (NoiseRows-1)/lodStepSizes[1] + 1,
                MapData = GetCollisionShapeMapData()
            };
        }

        // built-in function property cannot be changed from outside the main thread, hence CallDeferred, else set these in the main thread.
        if (lodIndex > 0)
            collider.SetDeferred("disabled", true);
        else
            collider.SetDeferred("disabled", false);
    }


    private float[] GetCollisionShapeMapData() {
        int lodStepSize = lodStepSizes[1];
        int pointsOnLine = (NoiseRows-1)/lodStepSize + 1;
        int totalPointsOnGrid = pointsOnLine*pointsOnLine;
        float[] arr = new float[totalPointsOnGrid];
        for (int j = 0; j < NoiseColumns; j+=lodStepSize)
            for (int i = 0; i < NoiseRows; i+=lodStepSize)
                arr[i/lodStepSize + j/lodStepSize*pointsOnLine] = noiseMap[i,j]*HeightLimit;
        return arr;
    }


    public void SetMaterial() {
        terrainChunkMesh.MaterialOverride = this.material;
    }


    public void SetTerrainMesh() {
        terrainChunkMesh.Mesh = cachedArrayMeshes[lodIndex];
    }


    public void SetCollisionShape() {
        collider.Shape = heightMapShape3D;
        collider.Scale = new Vector3(CellWidth*lodStepSizes[1], 1, CellWidth*lodStepSizes[1]);
    }

    
    public void AddCollider() {
        if (!isColliderAdded)
            this.AddChild(collider);
            collider.Owner = this;
            isColliderAdded = true;
    }
}