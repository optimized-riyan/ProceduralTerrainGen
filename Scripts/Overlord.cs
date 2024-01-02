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
	private int chunkId = 1;
	private Godot.Collections.Dictionary<Vector2I, TerrainChunk> chunkStorage;


	public Overlord() {
		playerChunkCoor = new Vector2I();
	}
	

	public override void _Ready() {
		chunkStorage = new Godot.Collections.Dictionary<Vector2I, TerrainChunk>();

		if (noise is not null)
			NMG = new NoiseMapGenerator(noise);
		else
			NMG = new NoiseMapGenerator(new FastNoiseLite() { NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin });
		
		terrainParameters = new TerrainParameters(NoiseRows, NoiseColumns, NoiseScale, CellWidth, HeightLimit, noise, HeightMask, ColorMask, NMG);
		terrainChunkScene = GD.Load<PackedScene>("res://Scenes/TerrainChunk.tscn");

		playerChunkCoor.X = Mathf.FloorToInt(player.Position.X/(NoiseRows*CellWidth));
		playerChunkCoor.Y = Mathf.FloorToInt(player.Position.Z/(NoiseColumns*CellWidth));
		if (chunkStorage.ContainsKey(playerChunkCoor)) {
			previousChunk = chunkStorage[playerChunkCoor];
		}
		else {
			previousChunk = createNewChunk(playerChunkCoor);
		}
		previousChunk.Position = new Vector3(playerChunkCoor.X*CellWidth*NoiseRows, 0, playerChunkCoor.Y*CellWidth*NoiseColumns);
		previousChunk.Visible = true;
		previousChunk.isUpdatePending = true;
		prevPlayerChunkCoor.X = playerChunkCoor.X;
		prevPlayerChunkCoor.Y = playerChunkCoor.Y;
	}


	public override void _Process(double delta) {
		playerChunkCoor.X = Mathf.FloorToInt(player.Position.X/(NoiseRows*CellWidth));
		playerChunkCoor.Y = Mathf.FloorToInt(player.Position.Z/(NoiseColumns*CellWidth));
		if (prevPlayerChunkCoor.X != playerChunkCoor.X || prevPlayerChunkCoor.Y != playerChunkCoor.Y) {
			previousChunk.Visible = false;
			TerrainChunk terrainChunk;
			if (chunkStorage.ContainsKey(playerChunkCoor)) {
				terrainChunk = chunkStorage[playerChunkCoor];
			}
			else {
				terrainChunk = createNewChunk(playerChunkCoor);
			}
			terrainChunk.Visible = true;
			previousChunk = terrainChunk;
			GD.Print(chunkStorage);
		}

		prevPlayerChunkCoor.X = playerChunkCoor.X;
		prevPlayerChunkCoor.Y = playerChunkCoor.Y;
	}


	private TerrainChunk createNewChunk(Vector2I chunkCoordinate) {
		TerrainChunk terrainChunk = terrainChunkScene.Instantiate<TerrainChunk>();
		terrainChunk.setTerrainParameters(terrainParameters);
		terrainChunk.setChunkParameters(chunkCoordinate, 0);
		terrainChunk.Position = new Vector3(chunkCoordinate.X*CellWidth*NoiseRows, 0, chunkCoordinate.Y*CellWidth*NoiseColumns);
		terrainChunk.Name = $"TerrainChunk{chunkId++}";
		GetNode("TerrainChunks").AddChild(terrainChunk);
		terrainChunk.Owner = this;
		chunkStorage.Add(chunkCoordinate, terrainChunk);
		return terrainChunk;
	}
}
