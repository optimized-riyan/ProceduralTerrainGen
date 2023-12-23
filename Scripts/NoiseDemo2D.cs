using Godot;

public partial class NoiseDemo2D : Sprite2D
{
	FastNoiseLite noise = new FastNoiseLite();
	int height = 10;
	int width = 10;
	float scale = 0.1F;
	float offsetX = 0F;
	float offsetY = 0F;

	private float[,] Generate2DNoise(int height, int width, float scale) {
		float [,] noiseMap = new float[height, width];

		for (int x = 0; x < height; x++) {
			for (int y = 0; y < width; y++) {
				noiseMap[x,y] = noise.GetNoise2D(x/scale, y/scale);
			}
		}

		return noiseMap;
	}

	public override void _Ready() {
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		float[,] noiseMap = Generate2DNoise(height, width, scale);
		GD.Print($"{noiseMap[9,9]}");
	}
}
