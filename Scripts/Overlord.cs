using Godot;
using Global;


[Tool]
public partial class Overlord : Node3D {

	[ExportGroup("Terrain Parameters")]
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
	private Vector2I playerChunkCoor; 
	private Vector2I prevPlayerChunkCoor;
	private TerrainChunk previousChunk;
	private NoiseMapGenerator NMG;
	private TerrainParameters terrainParameters;
	private PackedScene terrainChunkScene;
	private Godot.Collections.Dictionary<Vector2I, TerrainChunk> chunkStorage;


	public Overlord() {
		chunkStorage = new Godot.Collections.Dictionary<Vector2I, TerrainChunk>();
		playerChunkCoor = new Vector2I();
	}
    

    public override void _Ready() {
		// refreshChildren();

		if (noise is not null)
			NMG = new NoiseMapGenerator(noise);
		else
			NMG = new NoiseMapGenerator(new FastNoiseLite() { NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin });
		
		terrainParameters = new TerrainParameters(NoiseRows, NoiseColumns, NoiseScale, CellWidth, HeightLimit, noise, HeightMask, ColorMask, NMG);
		terrainChunkScene = GD.Load<PackedScene>("res://Scenes/TerrainChunk.tscn");

		playerChunkCoor.X = Mathf.FloorToInt(CellWidth*player.Position.X/NoiseRows);
		playerChunkCoor.Y = Mathf.FloorToInt(CellWidth*player.Position.Z/NoiseRows);
		if (chunkStorage.ContainsKey(playerChunkCoor)) {
			previousChunk = chunkStorage[playerChunkCoor];
		}
		else {
			previousChunk = createNewChunk(playerChunkCoor);
		}
		previousChunk.Position = new Vector3(playerChunkCoor.X*CellWidth*NoiseRows, 0, playerChunkCoor.Y*CellWidth*NoiseColumns);
		previousChunk.isVisible = true;
    }


    public override void _Process(double delta) {
		playerChunkCoor.X = Mathf.FloorToInt(CellWidth*player.Position.X/NoiseRows);
		playerChunkCoor.Y = Mathf.FloorToInt(CellWidth*player.Position.Z/NoiseRows);
		// GD.Print($"{playerChunkCoor} {player.Position}");
		if (prevPlayerChunkCoor.X != playerChunkCoor.X && prevPlayerChunkCoor.Y != playerChunkCoor.Y) {
			previousChunk.isVisible = false;
			TerrainChunk terrainChunk;
			if (chunkStorage.ContainsKey(playerChunkCoor)) {
				terrainChunk = chunkStorage[playerChunkCoor];
			}
			else {
				terrainChunk = createNewChunk(playerChunkCoor);
			}
			terrainChunk.isVisible = true;
			previousChunk = terrainChunk;
		}

		prevPlayerChunkCoor.X = playerChunkCoor.X;
		prevPlayerChunkCoor.Y = playerChunkCoor.Y;
	}


	private void refreshChildren() {
		Godot.Collections.Array<Node> terrainChunks = GetNode<Node3D>("TerrainChunks").GetChildren();
		foreach (Node terrainChunk in terrainChunks) 
			terrainChunk.QueueFree();
	}


	private TerrainChunk createNewChunk(Vector2I chunkCoordinate) {
		TerrainChunk terrainChunk = terrainChunkScene.Instantiate<TerrainChunk>();
		terrainChunk.setTerrainParameters(terrainParameters);
		terrainChunk.setChunkParameters(new Vector2(player.Position.X/NoiseRows, player.Position.Z/NoiseColumns), 0);
		terrainChunk.Position = new Vector3(playerChunkCoor.X*CellWidth*NoiseRows, 0, playerChunkCoor.Y*CellWidth*NoiseColumns);
		GetNode("TerrainChunks").AddChild(terrainChunk);
		terrainChunk.Owner = this;
		chunkStorage.Add(chunkCoordinate, terrainChunk);
		return terrainChunk;
	}
}