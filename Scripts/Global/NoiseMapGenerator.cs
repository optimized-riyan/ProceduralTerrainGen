using System.Reflection.Metadata.Ecma335;
using Godot;

namespace Global {
	public class NoiseMapGenerator {

		public FastNoiseLite noise;
		public Curve HeightMask;
		private float LowerLimit;
		private float UpperLimit;
		private float k1;
		private float k2;


		public NoiseMapGenerator(FastNoiseLite.NoiseTypeEnum noiseEnum, float LowerLimit, float UpperLimit) {
            noise = new FastNoiseLite {
                NoiseType = noiseEnum
            };
			this.LowerLimit = LowerLimit;
			this.UpperLimit = UpperLimit;
			this.k1 = (UpperLimit - LowerLimit)/2;
			this.k2 = (UpperLimit + LowerLimit)/2;
        }


		public NoiseMapGenerator() {
            noise = new FastNoiseLite {
                NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin
            };
			this.LowerLimit = 0F;
			this.UpperLimit = 1F;
			this.k1 = (UpperLimit - LowerLimit)/2;
			this.k2 = (UpperLimit + LowerLimit)/2;
        }


		public float[,] Generate2DNoiseMap(int length, int width, float offsetX, float offsetY, float scale) {
			float [,] noiseMap = new float[length, width];
			float noiseValue;

			for (int x = 0; x < length; x++) {
				for (int y = 0; y < width; y++) {
					noiseValue = noise.GetNoise2D((x+offsetX)/scale, (y+offsetY)/scale);
					if (HeightMask != null)
						noiseMap[x,y] = HeightMask.SampleBaked(noiseValue*k1 + k2);
					else
						noiseMap[x,y] = noiseValue*k1 + k2;
				}
			}

			return noiseMap;
		}


		public float GetNoiseAt(float x, float y, float scale) {
			float noiseValue = noise.GetNoise2D(x/scale, y/scale);
			if (HeightMask != null)
				return HeightMask.SampleBaked(noiseValue*k1 + k2);
			else
				return noiseValue*k1 + k2;
		}


		public float[,] Generate2DNoiseMap(int length, int width, float scale) {
			return Generate2DNoiseMap(length, width, 0, 0, scale);
		}


		public float[,] Generate2DNoiseMap(int length, int width) {
			return Generate2DNoiseMap(length, width, 0, 0, 0.1F);
		}
	}
}