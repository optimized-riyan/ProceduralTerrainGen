using Godot;
using Global;
using System.Collections.Generic;
using System.Threading;


// [Tool]
public partial class Overlord : Node3D {

    private struct ChunkQueueParams {
        public Vector2I chunkCoor;
        public float dist;

        public ChunkQueueParams(Vector2I chunkCoor, float dist) {
            this.chunkCoor = chunkCoor;
            this.dist = dist;
        }
    }


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
    [Export(PropertyHint.Range, "1,8,")]
    public byte _renderDistance = 4;
    private byte renderDistance = 4;
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
    private Queue<ChunkQueueParams> chunkGenQueue;
    private List<Vector2I> lodArray;
    private HashSet<Vector2I> chunksToRender;
    private HashSet<Vector2I> chunksUnderGen;
    [Export]
    private Node3D chunksDirectory;
    private bool killChunkThread = false;
    private Thread chunkGenThread;


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
        terrainChunk.SetCollisionShape();
        terrainChunk.AddCollider();
    }


    public Overlord() {
        playerChunkCoor = new Vector2I();
        chunkStorage = new Godot.Collections.Dictionary<Vector2I, TerrainChunk>();
        renderedChunks = new HashSet<TerrainChunk>();
        chunkCallbackQueue = new Queue<TerrainChunk>();
        chunkGenQueue = new Queue<ChunkQueueParams>();
        chunksToRender = new HashSet<Vector2I>();
        chunksUnderGen = new HashSet<Vector2I>();
    }


    public override void _Ready() {
        NMG = new NoiseMapGenerator(noise);
        LoadResourcesAndNodePaths();
        NMG.SetSeed(NoiseSeed);
        renderDistance = _renderDistance;
        UpdateLODArray();

        terrainParameters = new TerrainParameters(NoiseRows, NoiseColumns, NoiseScale, CellWidth, HeightLimit, NoiseSeed, noise, HeightMask, ColorMask, NMG);
        terrainChunkScene = GD.Load<PackedScene>("res://Scenes/TerrainChunk.tscn");

        StartChunkGenThread();
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

        // if (player.Position.Y < -100)
        //     player.Position = new Vector3(player.Position.X, HeightLimit, player.Position.Z);

        playerChunkCoor.X = Mathf.FloorToInt(player.Position.X/(NoiseRows*CellWidth));
        playerChunkCoor.Y = Mathf.FloorToInt(player.Position.Z/(NoiseColumns*CellWidth));

        if (prevPlayerChunkCoor.X != playerChunkCoor.X || prevPlayerChunkCoor.Y != playerChunkCoor.Y) {
            UpdateChunks();
        }

        prevPlayerChunkCoor.X = playerChunkCoor.X;
        prevPlayerChunkCoor.Y = playerChunkCoor.Y;
    }


    private void StartChunkGenThread() {
        ThreadStart threadStart = delegate {
            while (!killChunkThread) {
                if (chunkGenQueue.Count > 0) {
                    ChunkQueueParams chunk = chunkGenQueue.Dequeue();
                    bool isPresent;
                    TerrainChunk terrainChunk;
                    lock (chunkStorage) { isPresent = chunkStorage.ContainsKey(chunk.chunkCoor); }
                    if (isPresent)
                        terrainChunk = chunkStorage[chunk.chunkCoor];
                    else
                        terrainChunk = CreateNewChunk(chunk.chunkCoor);
                    terrainChunk.UpdateLOD(lodCurve.SampleBaked(chunk.dist));
                    lock (chunkCallbackQueue) { chunkCallbackQueue.Enqueue(terrainChunk); }
                }
            }
        };
        Thread thread = new Thread(threadStart);
        chunkGenThread = thread;
        thread.Start();
    }


    private void LoadResourcesAndNodePaths() {
        noise = GD.Load<FastNoiseLite>("res://Resources/TerrainNoise.tres");
        HeightMask = GD.Load<Curve>("res://Resources/HeightMask.tres");
        ColorMask = GD.Load<Gradient>("res://Resources/ColorMask.tres");
        lodCurve = GD.Load<Curve>("res://Resources/LodCurve.tres");
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
            lock (chunkGenQueue) { chunkGenQueue.Enqueue(new ChunkQueueParams(chunkCoor, ((float)(currentI*currentI + currentJ*currentJ))/(renderDistance*renderDistance))); }
        }
    }


    private TerrainChunk CreateNewChunk(Vector2I chunkCoordinate) {
        TerrainChunk terrainChunk = terrainChunkScene.Instantiate<TerrainChunk>();
        terrainChunk.SetTerrainParameters(terrainParameters);
        terrainChunk.SetChunkParameters(chunkCoordinate);
        terrainChunk.OnNew();
        chunksDirectory.CallDeferred("add_child", terrainChunk);
        terrainChunk.SetDeferred("owner", this);
        lock (chunkStorage) chunkStorage.Add(chunkCoordinate, terrainChunk);
        return terrainChunk;
    }


    public void SetSeed(int seed) { NoiseSeed = seed; }


    public void SetPlayerSpeed(double speed) { GetNode<CharacterBody3D>("Explorer").Set("movement_speed", speed); }


    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey eventKey)
            if (eventKey.Pressed && eventKey.KeyLabel == Key.Escape)
                EmitSignal(SignalName.GamePauseToggled);
    }


    public override void _ExitTree() {
        killChunkThread = true;
        chunkGenThread.Join();
    }
}