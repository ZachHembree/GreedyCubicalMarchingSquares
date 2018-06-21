using UnityEngine;

public class MapGenerator
{
    private int length, width, height;
    private bool useRandom, smoothing;
    private string seed;
    private float[][][] map;

    public MapGenerator(int _length, int _width, int _height, bool _useRandom, bool _smoothing, string _seed)
    {
        length = _length;
        width = _width;
        height = _height;
        useRandom = _useRandom;
        smoothing = _smoothing;
        seed = _seed;
    }

    public float[][][] GenerateMap()
    {
        map = new float[length][][];
		
        GetRandomElevations();
        if (smoothing) SmoothSurface();
		
        return map;
    }

    private void GetRandomElevations()
    {
        float average = 0;

        if (useRandom) GetSeed();
        System.Random rand = new System.Random(seed.GetHashCode());

        for (int x = 0; x < length; x++)
        {
            map[x] = new float[width][];

            for (int y = 0; y < width; y++)
            {
                map[x][y] = new float[1];

                float z = (rand.Next(1000, height * 1000) / 1000f) + 1.0f;
                //float z = rand.Next(1, height);
                map[x][y][0] = z;
                average += z;
            }
        }

        average /= (length * width);
        map[0][0][0] = average;
    }

    private void GetSeed()
    {
        string trimSeed = Time.realtimeSinceStartup.ToString();
        seed = "";

        for (int n = 2; n < trimSeed.Length; n++) seed += trimSeed[n];
    }

    private void SmoothSurface()
    {
        float zDelta;

        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < width; y++)
            {
                for (int a = 0; a < 2; a++)
                {
                    for (int b = 0; b < 2; b++)
                    {
                        if ((x + a < length && x + a > -1) && (y + b < width && y + b > -1))
                        {
                            zDelta = map[x + a][y + b][0] - map[x][y][0];

                            if (zDelta > 0)
                            {
                                if (map[x][y][0] + 1.0f < height)
                                    map[x + a][y + b][0] = map[x][y][0] + 0.25f;
                                else
                                    map[x + a][y + b] = map[x][y];
                            }
                            else if (zDelta < 0.0f)
                            {
                                if (map[x][y][0] - 1.0f > 0.0f)
                                    map[x + a][y + b][0] = map[x][y][0] - 0.25f;
                                else
                                    map[x + a][y + b] = map[x][y];
                            }
                        }
                    }
                }
            }
        }
    }
}
