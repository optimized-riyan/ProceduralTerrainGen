using Godot;
using Global;
using System.Collections.Generic;


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
    private byte renderDistance;
    private Vector2I playerChunkCoor; 
    private Vector2I prevPlayerChunkCoor;
    private List<TerrainChunk> renderedChunks;
    private NoiseMapGenerator NMG;
    private TerrainParameters terrainParameters;
    private PackedScene terrainChunkScene;
    private int chunkId = 1;
    private Godot.Collections.Dictionary<Vector2I, TerrainChunk> chunkStorage;


    public Overlord() {
        playerChunkCoor = new Vector2I();
        chunkStorage = new Godot.Collections.Dictionary<Vector2I, TerrainChunk>();
        renderedChunks = new List<TerrainChunk>();
    }


    public override void _Ready() {
        chunkStorage.Clear();

        if (noise is not null)
            NMG = new NoiseMapGenerator(noise);
        else
            NMG = new NoiseMapGenerator(new FastNoiseLite() { NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin });
        
        terrainParameters = new TerrainParameters(NoiseRows, NoiseColumns, NoiseScale, CellWidth, HeightLimit, noise, HeightMask, ColorMask, NMG);
        terrainChunkScene = GD.Load<PackedScene>("res://Scenes/TerrainChunk.tscn");

        playerChunkCoor.X = Mathf.FloorToInt(player.Position.X/(NoiseRows*CellWidth));
        playerChunkCoor.Y = Mathf.FloorToInt(player.Position.Z/(NoiseColumns*CellWidth));

        updateChunks();

        prevPlayerChunkCoor.X = playerChunkCoor.X;
        prevPlayerChunkCoor.Y = playerChunkCoor.Y;
    }


    public override void _Process(double delta) {
        playerChunkCoor.X = Mathf.FloorToInt(player.Position.X/(NoiseRows*CellWidth));
        playerChunkCoor.Y = Mathf.FloorToInt(player.Position.Z/(NoiseColumns*CellWidth));

        if (prevPlayerChunkCoor.X != playerChunkCoor.X || prevPlayerChunkCoor.Y != playerChunkCoor.Y)
            updateChunks();

        prevPlayerChunkCoor.X = playerChunkCoor.X;
        prevPlayerChunkCoor.Y = playerChunkCoor.Y;
    }


    private void updateChunks() {
        foreach (TerrainChunk t in renderedChunks)
            t.Visible = false;
        renderedChunks.Clear();
        
        TerrainChunk terrainChunk;
        Vector2I chunkCoor;
        for (int i = 0; i <= renderDistance; i++) {
            float rangeJ = Mathf.FloorToInt(Mathf.Sqrt(renderDistance*renderDistance - i*i));
            for (int j = 0; j < rangeJ; j++) {
                chunkCoor = new Vector2I(playerChunkCoor.X + i, playerChunkCoor.Y + j);
                terrainChunk = chunkStorage.ContainsKey(chunkCoor) ? chunkStorage[chunkCoor] : createNewChunk(chunkCoor, 0);
                terrainChunk.Visible = true;
                renderedChunks.Add(terrainChunk);

                chunkCoor = new Vector2I(playerChunkCoor.X - i, playerChunkCoor.Y - j);
                terrainChunk = chunkStorage.ContainsKey(chunkCoor) ? chunkStorage[chunkCoor] : createNewChunk(chunkCoor, 0);
                terrainChunk.Visible = true;
                renderedChunks.Add(terrainChunk);
                
                chunkCoor = new Vector2I(playerChunkCoor.X + i, playerChunkCoor.Y - j);
                terrainChunk = chunkStorage.ContainsKey(chunkCoor) ? chunkStorage[chunkCoor] : createNewChunk(chunkCoor, 0);
                terrainChunk.Visible = true;
                renderedChunks.Add(terrainChunk);

                chunkCoor = new Vector2I(playerChunkCoor.X - i, playerChunkCoor.Y + j);
                terrainChunk = chunkStorage.ContainsKey(chunkCoor) ? chunkStorage[chunkCoor] : createNewChunk(chunkCoor, 0);
                terrainChunk.Visible = true;
                renderedChunks.Add(terrainChunk);
            }
        }
    }


    private TerrainChunk createNewChunk(Vector2I chunkCoordinate, int lodIndex) {
        TerrainChunk terrainChunk = terrainChunkScene.Instantiate<TerrainChunk>();
        terrainChunk.setTerrainParameters(terrainParameters);
        terrainChunk.setChunkParameters(chunkCoordinate, lodIndex);
        terrainChunk.Position = new Vector3(chunkCoordinate.X*CellWidth*NoiseRows, 0, chunkCoordinate.Y*CellWidth*NoiseColumns);
        terrainChunk.Name = $"TerrainChunk{chunkId++}";
        GetNode("TerrainChunks").AddChild(terrainChunk);
        terrainChunk.Owner = this;
        chunkStorage.Add(chunkCoordinate, terrainChunk);
        return terrainChunk;
    }
}