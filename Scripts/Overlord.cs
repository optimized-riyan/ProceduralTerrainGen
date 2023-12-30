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
	private NoiseMapGenerator NMG;
	private TerrainParameters terrainParameters;


    public override void _Ready() {
		if (noise is not null)
			NMG = new NoiseMapGenerator(noise);
		else
			NMG = new NoiseMapGenerator(new FastNoiseLite() { NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin });
		
		terrainParameters = new TerrainParameters(NoiseRows, NoiseColumns, NoiseScale, CellWidth, HeightLimit, noise, HeightMask, ColorMask, NMG);
		
		PackedScene terrainChunkScene = GD.Load<PackedScene>("res://Scenes/TerrainChunk.tscn");
		TerrainChunk terrainChunk = terrainChunkScene.Instantiate<TerrainChunk>();
		terrainChunk.setTerrainParameters(terrainParameters);
		GetNode("TerrainChunks").AddChild(terrainChunk);
		terrainChunk.Owner = this;
    }


    public override void _Process(double delta) {
		
	}
}
