using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEngine;

public class DisplayTextureOnPlane : MonoBehaviour
{
    public int size;

    [Range(1.0f, 50.0f)]
    public float scale;

    public Vector3 offset;

    public void display()
    {
        float[,,] noiseMap = NoiseGenerator.generateNoiseCubemap(size, scale, offset);
        Cubemap noiseTex = mapToCubemap(noiseMap, size);

        Renderer textureRenderer = GetComponent<MeshRenderer>();
        textureRenderer.sharedMaterial.mainTexture = noiseTex;
    }

    private Cubemap mapToCubemap(float[,,] map, int size)
    {
        Color[][] colors = new Color[6][];

        for (int m = 0; m < 6; m++)
        {
            colors[m] = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    colors[m][x + y*size] = Color.Lerp(Color.black, Color.white, (map[m, x, y] + 1.0f) / 2.0f);
                }
            }
        }

        Cubemap cubemap = new Cubemap(size, TextureFormat.RGB24, true);

        cubemap.SetPixels(colors[0], CubemapFace.PositiveX);
        cubemap.SetPixels(colors[1], CubemapFace.NegativeX);
        cubemap.SetPixels(colors[2], CubemapFace.PositiveY);
        cubemap.SetPixels(colors[3], CubemapFace.NegativeY);
        cubemap.SetPixels(colors[4], CubemapFace.PositiveZ);
        cubemap.SetPixels(colors[5], CubemapFace.NegativeZ);
        cubemap.Apply();
        
        cubemap.name = "Perlin Noise Cubemap";
        cubemap.wrapMode = TextureWrapMode.Clamp;
        cubemap.filterMode = FilterMode.Point;

        return cubemap;
    }

    private Texture2D mapToTexture(float[,] map, int size)
    {
        Color[] colors = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                colors[x + y * size] = Color.Lerp(Color.black, Color.white, (map[x, y] + 1.0f)/2.0f);
            }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true);
        texture.SetPixels(colors);
        texture.Apply();
        texture.name = "Perlin Noise";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    void Start()
    {
        display();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
