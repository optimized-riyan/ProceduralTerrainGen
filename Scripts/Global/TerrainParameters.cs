using Godot;


namespace TerrainParameters {
    public static class TerrainParameters {
        static int NoiseRows = 181;
        static private int NoiseColumns = 181;
        static private float NoiseScale = 0.35F;
        static private float CellWidth = 4F;
        static private float HeightLimit = 100F;
        static private int NoiseSeed = 0;
        static private FastNoiseLite noise;
        static private Curve HeightMask;
        static private Gradient ColorMask;
        static private float[,] noiseMap;
        static private Vector2 chunkCoordinate;
        static public int lodIndex = 0;
        static private int[] lodStepsSizes = new int[]{1, 4, 8, 12, 18, 30};
        static private bool Update = false;
    }
}