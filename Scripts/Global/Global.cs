using Godot;

namespace Global {

	public struct TerrainParameters {
		public int NoiseRows = 181;
		public int NoiseColumns = 181;
		public float NoiseScale = 0.35F;
		public float CellWidth = 4F;
		public float HeightLimit = 100F;
		public int NoiseSeed = 0;
		public FastNoiseLite noise;
		public Curve HeightMask;
		public Gradient ColorMask;
		public NoiseMapGenerator NMG;

        public TerrainParameters(int NoiseRows, int NoiseColumns, float NoiseScale, float CellWidth, float HeightLimit, int NoiseSeed, FastNoiseLite noise, Curve HeightMask, Gradient ColorMask, NoiseMapGenerator NMG) {
			this.NoiseRows = NoiseRows;
			this.NoiseColumns = NoiseColumns;
			this.NoiseScale = NoiseScale;
			this.CellWidth = CellWidth;
			this.HeightLimit = HeightLimit;
			this.NoiseSeed = NoiseSeed;
			this.noise = noise;
			this.HeightMask = HeightMask;
			this.ColorMask = ColorMask;
			this.NMG = NMG;
        }
    }



	public class NoiseMapGenerator {

		private FastNoiseLite noise;
		public Curve HeightMask;
		private float LowerLimit;
		private float UpperLimit;
		private float k1;
		private float k2;


		public NoiseMapGenerator(FastNoiseLite noise, float LowerLimit, float UpperLimit) {
			this.noise = noise;
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


		public NoiseMapGenerator(FastNoiseLite noise) {
			this.noise = noise;
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
					float remappedNoiseVal = noiseValue*k1 + k2;
					if (HeightMask != null)
						// noiseMap[x,y] = Mathf.Min(HeightMask.SampleBaked(remappedNoiseVal), remappedNoiseVal);
						noiseMap[x,y] = HeightMask.SampleBaked(remappedNoiseVal);
					else
						noiseMap[x,y] = remappedNoiseVal;
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


		public void SetSeed(int seed) {
			noise.Seed = seed;
		}
	}
}