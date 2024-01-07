using Godot;
using Global;
using System.Collections.Generic;
using System.Threading;


[Tool]
public partial class Overlord : Node3D {

    [ExportGroup("Terrain Parameters")]
	[Export(PropertyHint.Range, "2,181,")]
    private int NoiseRows = 181;
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
    private Node3D player;
    [Export(PropertyHint.Range, "1,16,")]
    public byte _renderDistance;
    private byte renderDistance;
    [Export]
    private Curve lodCurve;     // lower the value higher the detail
    private Vector2I playerChunkCoor; 
    private Vector2I prevPlayerChunkCoor;
    private HashSet<TerrainChunk> renderedChunks;
    private NoiseMapGenerator NMG;
    private TerrainParameters terrainParameters;
    private PackedScene terrainChunkScene;
    private int chunkId = 1;
    private Godot.Collections.Dictionary<Vector2I, TerrainChunk> chunkStorage;
    private Queue<TerrainChunk> chunkCallbackQueue;
    private List<Vector2I> lodArray;
    private HashSet<Vector2I> chunksToRender;
    private HashSet<Vector2I> chunksUnderGen;
    [Export]
    private Node3D chunksDirectory;


    [Signal]
    public delegate void GamePauseToggledEventHandler();


    void OnTerrainChunkLoaded(TerrainChunk terrainChunk) {
        bool isPresent;
        lock (chunksToRender) isPresent = chunksToRender.Contains(terrainChunk.chunkCoordinate);
        if (isPresent) {
            terrainChunk.Visible = true;
            lock (renderedChunks) renderedChunks.Add(terrainChunk);
        }
        else {
            terrainChunk.Visible = false;
        }
        lock(chunksUnderGen) chunksUnderGen.Remove(terrainChunk.chunkCoordinate);
        terrainChunk.SetDeferred("name", $"TerrainChunk{chunkId++}");
        terrainChunk.SetTerrainMesh();
        terrainChunk.SetMaterial();
    }


    public Overlord() {
        playerChunkCoor = new Vector2I();
        chunkStorage = new Godot.Collections.Dictionary<Vector2I, TerrainChunk>();
        renderedChunks = new HashSet<TerrainChunk>();
        chunkCallbackQueue = new Queue<TerrainChunk>();
        chunksToRender = new HashSet<Vector2I>();
        chunksUnderGen = new HashSet<Vector2I>();
    }


    public override void _Ready() {
        lock(chunkStorage) { chunkStorage.Clear(); }
        renderDistance = _renderDistance;
        UpdateLODArray();

        if (noise is not null)
            NMG = new NoiseMapGenerator(noise);
        else
            NMG = new NoiseMapGenerator(new FastNoiseLite() { NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin });
        
        terrainParameters = new TerrainParameters(NoiseRows, NoiseColumns, NoiseScale, CellWidth, HeightLimit, noise, HeightMask, ColorMask, NMG);
        terrainChunkScene = GD.Load<PackedScene>("res://Scenes/TerrainChunk.tscn");

        playerChunkCoor.X = Mathf.FloorToInt(player.Position.X/(NoiseRows*CellWidth));
        playerChunkCoor.Y = Mathf.FloorToInt(player.Position.Z/(NoiseColumns*CellWidth));

        UpdateChunks();

        prevPlayerChunkCoor.X = playerChunkCoor.X;
        prevPlayerChunkCoor.Y = playerChunkCoor.Y;
    }


    public override void _Process(double delta) {
        if (_renderDistance != renderDistance) {
            renderDistance = _renderDistance;
            UpdateLODArray();
        }

        while (chunkCallbackQueue.Count > 0) {
            lock (chunkCallbackQueue) {
                OnTerrainChunkLoaded(chunkCallbackQueue.Dequeue());
            }
        }

        playerChunkCoor.X = Mathf.FloorToInt(player.Position.X/(NoiseRows*CellWidth));
        playerChunkCoor.Y = Mathf.FloorToInt(player.Position.Z/(NoiseColumns*CellWidth));

        if (prevPlayerChunkCoor.X != playerChunkCoor.X || prevPlayerChunkCoor.Y != playerChunkCoor.Y) {
            UpdateChunks();
        }

        prevPlayerChunkCoor.X = playerChunkCoor.X;
        prevPlayerChunkCoor.Y = playerChunkCoor.Y;
    }


    private void UpdateLODArray() {
        lodArray = new List<Vector2I>();
        for (int i = -renderDistance+1; i <= renderDistance-1; i++) {
            int rangeJ = renderDistance-Mathf.Abs(i)-1;
            for (int j = -rangeJ; j <= rangeJ; j++)
                lodArray.Add(new Vector2I(i, j));
        }

        if (renderDistance == 1) lodArray.Add(new Vector2I(0, 0));
    }


    private void UpdateChunks() {
        lock (renderedChunks) {
            foreach (TerrainChunk t in renderedChunks) {
                if (!chunksToRender.Contains(t.chunkCoordinate)) {
                    t.Visible = false;
                    renderedChunks.Remove(t);
                }
            }
        }
        lock(chunksToRender) { chunksToRender.Clear(); }

        foreach (Vector2I vector in lodArray) {
            int currentI = vector.X;
            int currentJ = vector.Y;
            Vector2I chunkCoor = new Vector2I(playerChunkCoor.X + currentI, playerChunkCoor.Y + currentJ);
            lock (chunksToRender) { chunksToRender.Add(chunkCoor); }
            lock (chunksUnderGen) {
                if (chunksUnderGen.Contains(chunkCoor)) continue;
                chunksUnderGen.Add(chunkCoor);
            }
            if (chunkStorage.ContainsKey(chunkCoor)) {
                ThreadStart threadStart = delegate {
                    TerrainChunk terrainChunk = chunkStorage[chunkCoor];
                    terrainChunk.UpdateLOD(lodCurve.SampleBaked(((float)(currentI*currentI + currentJ*currentJ))/(renderDistance*renderDistance)));
                    lock (chunkCallbackQueue) { chunkCallbackQueue.Enqueue(terrainChunk); }
                };
                new Thread(threadStart).Start();
            }
            else {
                ThreadStart threadStart = delegate {
                    TerrainChunk terrainChunk = CreateNewChunk(chunkCoor, chunkId);
                    terrainChunk.UpdateLOD(lodCurve.SampleBaked(((float)(currentI*currentI + currentJ*currentJ))/(renderDistance*renderDistance)));
                    lock (chunkCallbackQueue) { chunkCallbackQueue.Enqueue(terrainChunk); }
                };
                new Thread(threadStart).Start();
            }
        }
    }


    private TerrainChunk CreateNewChunk(Vector2I chunkCoordinate, int chunkId) {
        TerrainChunk terrainChunk = terrainChunkScene.Instantiate<TerrainChunk>();
        terrainChunk.SetTerrainParameters(terrainParameters);
        terrainChunk.SetChunkParameters(chunkCoordinate);
        terrainChunk.OnNew();
        chunksDirectory.CallDeferred("add_child", terrainChunk);
        terrainChunk.SetDeferred("owner", this);
        lock (chunkStorage) chunkStorage.Add(chunkCoordinate, terrainChunk);
        return terrainChunk;
    }


    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey eventKey)
            if (eventKey.Pressed && eventKey.KeyLabel == Key.Escape)
                EmitSignal(SignalName.GamePauseToggled);
    }
}