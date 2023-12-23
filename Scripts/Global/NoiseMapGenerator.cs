using Godot;

namespace Global {
	public class NoiseMapGenerator {

		FastNoiseLite noise;


		public NoiseMapGenerator(FastNoiseLite.NoiseTypeEnum noiseEnum) {
            noise = new FastNoiseLite {
                NoiseType = noiseEnum
            };
        }


		public NoiseMapGenerator() {
            noise = new FastNoiseLite {
                NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin
            };
        }


		public float[,] Generate2DNoiseMap(int height, int width, float offsetX, float offsetY, float scale) {
			float [,] noiseMap = new float[height, width];

			for (int x = 0; x < height; x++) {
				for (int y = 0; y < width; y++) {
					noiseMap[x,y] = noise.GetNoise2D((x+offsetX)/scale, (y+offsetY)/scale);
				}
			}

			return noiseMap;
		}

		public float[,] Generate2DNoiseMap(int height, int width, float scale) {
			return Generate2DNoiseMap(height, width, 0, 0, scale);
		}

		public float[,] Generate2DNoiseMap(int height, int width) {
			return Generate2DNoiseMap(height, width, 0, 0, 0.1F);
		}
	}
}