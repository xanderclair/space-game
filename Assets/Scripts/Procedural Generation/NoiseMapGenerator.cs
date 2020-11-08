using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public static class NoiseMapGenerator
{
    public static float[,] generate(int width, int height, int seed, float scale, Vector2 offset, int octaves, float persistence, float lacunarity)
    {
        if (scale <= 0)
            scale = 0.0001f;
        float[,] noise = new float[width, height];

        float min = 0.0f;
        float max = 0.0f;

        float aspect = (float)Math.Max(width, height);

        System.Random rand = new System.Random(seed);
        Vector2[] offsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float x = rand.Next(-10000, 10000);
            float y = rand.Next(-10000, 10000);
            offsets[i] = new Vector2(x, y);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = 0.0f;

                for (int i = 0; i < octaves; i++)
                {
                    float scaleMultiplier = Mathf.Pow(lacunarity, i);
                    float ptX = offset.x*scaleMultiplier + offsets[i].x + (x/aspect) * scale * scaleMultiplier;
                    float ptY = offset.y*scaleMultiplier + offsets[i].y + (y/aspect) * scale * scaleMultiplier;

                    noiseValue += (-1.0f + 2.0f*Mathf.PerlinNoise(ptX, ptY)) * Mathf.Pow(persistence, i);
                }
                min = Mathf.Min(min, noiseValue);
                max = Mathf.Max(max, noiseValue);
                noise[x, y] = noiseValue;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                noise[x, y] = Mathf.InverseLerp(min, max, noise[x, y]);
            }
        }

        return noise;
    }

    public static Texture2D mapToTexture(float[,] noiseMap, int width, int height)
    {
        Texture2D noiseTex = new Texture2D(width, height);

        Color[] colors = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colors[x + y*width] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);
            }
        }

        noiseTex.SetPixels(colors);
        noiseTex.Apply();
        return noiseTex;
    }
}
