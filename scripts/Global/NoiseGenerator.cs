using Godot;

public partial class NoiseGenerator {

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
}