using System;
using System.Collections;
using UnityEngine;


public static class NoiseGenerator
{
    private static int[] permutationTable =
    {
        151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
        140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
        247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
         57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
         74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
         60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
         65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
        200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
         52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
        207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
        119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
        129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
        218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
         81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
        184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
        222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180,

        151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
        140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
        247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
         57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
         74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
         60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
         65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
        200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
         52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
        207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
        119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
        129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
        218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
         81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
        184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
        222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180
    };

    private static int hashMask = 255;
    private static float root2 = Mathf.Sqrt(2.0f);

    private static Vector2[] gradients2D = // 8 vectors for the 4 2d axis-aligned vectors and the diagonals
    {
        new Vector2( 1.0f, 0.0f),
        new Vector2(-1.0f, 0.0f),
        new Vector2( 0.0f, 1.0f),
        new Vector2( 0.0f,-1.0f),
        new Vector2( 1.0f, 1.0f).normalized,
        new Vector2(-1.0f, 1.0f).normalized,
        new Vector2( 1.0f,-1.0f).normalized,
        new Vector2(-1.0f,-1.0f).normalized,
    };

    private static Vector3[] gradients3D =
    {
        new Vector3( 1.0f, 1.0f, 0.0f),
        new Vector3(-1.0f, 1.0f, 0.0f),
        new Vector3( 1.0f,-1.0f, 0.0f),
        new Vector3(-1.0f,-1.0f, 0.0f),
        new Vector3( 1.0f, 0.0f, 1.0f),
        new Vector3(-1.0f, 0.0f, 1.0f),
        new Vector3( 1.0f, 0.0f,-1.0f),
        new Vector3(-1.0f, 0.0f,-1.0f),
        new Vector3( 0.0f, 1.0f, 1.0f),
        new Vector3( 0.0f,-1.0f, 1.0f),
        new Vector3( 0.0f, 1.0f,-1.0f),
        new Vector3( 0.0f,-1.0f,-1.0f),

        new Vector3( 1.0f, 1.0f, 0.0f),
        new Vector3(-1.0f, 1.0f, 0.0f),
        new Vector3( 0.0f,-1.0f, 1.0f),
        new Vector3( 0.0f,-1.0f,-1.0f)
    };

    private static Vector3[] cubemapOrder =
    {
        new Vector3( 1.0f, 0.0f, 0.0f), // +X right
        new Vector3( 0.0f, 0.0f,-1.0f),
        new Vector3( 0.0f,-1.0f, 0.0f),

        new Vector3(-1.0f, 0.0f, 0.0f), // -X left
        new Vector3( 0.0f, 0.0f, 1.0f),
        new Vector3( 0.0f,-1.0f, 0.0f),

        new Vector3( 0.0f, 1.0f, 0.0f), // +Y top
        new Vector3( 1.0f, 0.0f, 0.0f),
        new Vector3( 0.0f, 0.0f, 1.0f),

        new Vector3( 0.0f,-1.0f, 0.0f), // -Y bottom
        new Vector3( 1.0f, 0.0f, 0.0f),
        new Vector3( 0.0f, 0.0f,-1.0f),

        new Vector3( 0.0f, 0.0f, 1.0f), // +Z back (forward? but if you're looking from negative z, this face would be behind)
        new Vector3( 1.0f, 0.0f, 0.0f),
        new Vector3( 0.0f,-1.0f, 0.0f),

        new Vector3( 0.0f, 0.0f,-1.0f), // -Z front
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3( 0.0f,-1.0f, 0.0f)
    };

    public static float[,] generateNoiseMap2D(int size, float scale)
    {
        float[,] noise = new float[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pt = new Vector2(x, y);
                pt *= (1.0f / size) * scale;

                noise[x, y] = sampleNoise2D(pt);
            }
        }

        return noise;
    }

    public static float[,,] generateNoiseCubemap(int size, float scale, Vector3 offset)
    {
        float[,,] cubemap = new float[6, size, size];

        for (int m = 0; m < 6; m++)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Vector2 pt2D = new Vector2(x, y);
                    pt2D *= (1.0f / (size+1.0f)) * 2.0f;
                    pt2D += new Vector2(-1.0f, -1.0f) * ((size - 1.0f) / size); ;

                    Vector3 pt = cubemapOrder[m * 3];
                    pt += cubemapOrder[m * 3 + 1] * pt2D.x;
                    pt += cubemapOrder[m * 3 + 2] * pt2D.y;
                    pt = pt.normalized;

                    pt *= scale;

                    pt += offset;

                    cubemap[m, x, y] = sampleNoise3D(pt);
                }
            }
        }

        return cubemap;
    }

    public static float sampleNoise2D(Vector2 point)
    {
        Vector2Int bottomLeft = Vector2Int.FloorToInt(point);
        Vector2 fractionalPart = point - bottomLeft;

        bottomLeft.x &= hashMask;
        bottomLeft.y &= hashMask;

        Vector2Int topRight = bottomLeft + new Vector2Int(1, 1);

        int hash0 = permutationTable[bottomLeft.x];
        int hash1 = permutationTable[topRight.x];

        Vector2 gradientBL = gradients2D[permutationTable[hash0 + bottomLeft.y] & 7];
        Vector2 gradientBR = gradients2D[permutationTable[hash1 + bottomLeft.y] & 7];
        Vector2 gradientTL = gradients2D[permutationTable[hash0 + topRight.y] & 7];
        Vector2 gradientTR = gradients2D[permutationTable[hash1 + topRight.y] & 7];

        float dotBL = Vector2.Dot(gradientBL, fractionalPart);
        float dotBR = Vector2.Dot(gradientBR, new Vector2(fractionalPart.x - 1.0f, fractionalPart.y));
        float dotTL = Vector2.Dot(gradientTL, new Vector2(fractionalPart.x, fractionalPart.y - 1.0f));
        float dotTR = Vector2.Dot(gradientTR, new Vector2(fractionalPart.x - 1.0f, fractionalPart.y - 1.0f));

        return  (Mathf.SmoothStep(
                Mathf.SmoothStep(dotBL, dotBR, fractionalPart.x),
                Mathf.SmoothStep(dotTL, dotTR, fractionalPart.x),
                fractionalPart.y) * root2);
    }

    public static float sampleNoise3D(Vector3 point)
    {
        Vector3Int cube000 = Vector3Int.FloorToInt(point);
        Vector3 fractionalPart = point - cube000;
        Vector3 fractionalPartMinusOne = fractionalPart - new Vector3(1.0f, 1.0f, 1.0f);

        cube000.x &= hashMask;
        cube000.y &= hashMask;
        cube000.z &= hashMask;

        Vector3Int cube111 = cube000 + new Vector3Int(1, 1, 1);

        int hash0 = permutationTable[cube000.x];
        int hash1 = permutationTable[cube111.x];

        int hash00 = permutationTable[hash0 + cube000.y];
        int hash10 = permutationTable[hash1 + cube000.y];
        int hash01 = permutationTable[hash0 + cube111.y];
        int hash11 = permutationTable[hash1 + cube111.y];

        Vector3 gradient000 = gradients3D[permutationTable[hash00 + cube000.z] & 15];
        Vector3 gradient100 = gradients3D[permutationTable[hash10 + cube000.z] & 15];
        Vector3 gradient010 = gradients3D[permutationTable[hash01 + cube000.z] & 15];
        Vector3 gradient110 = gradients3D[permutationTable[hash11 + cube000.z] & 15];
        Vector3 gradient001 = gradients3D[permutationTable[hash00 + cube111.z] & 15];
        Vector3 gradient101 = gradients3D[permutationTable[hash10 + cube111.z] & 15];
        Vector3 gradient011 = gradients3D[permutationTable[hash01 + cube111.z] & 15];
        Vector3 gradient111 = gradients3D[permutationTable[hash11 + cube111.z] & 15];

        float dot000 = Vector3.Dot(gradient000, fractionalPart);
        float dot100 = Vector3.Dot(gradient100, new Vector3(fractionalPartMinusOne.x, fractionalPart.y, fractionalPart.z));
        float dot010 = Vector3.Dot(gradient010, new Vector3(fractionalPart.x, fractionalPartMinusOne.y, fractionalPart.z));
        float dot110 = Vector3.Dot(gradient110, new Vector3(fractionalPartMinusOne.x, fractionalPartMinusOne.y, fractionalPart.z));
        float dot001 = Vector3.Dot(gradient001, new Vector3(fractionalPart.x, fractionalPart.y, fractionalPartMinusOne.z));
        float dot101 = Vector3.Dot(gradient101, new Vector3(fractionalPartMinusOne.x, fractionalPart.y, fractionalPartMinusOne.z));
        float dot011 = Vector3.Dot(gradient011, new Vector3(fractionalPart.x, fractionalPartMinusOne.y, fractionalPartMinusOne.z));
        float dot111 = Vector3.Dot(gradient111, fractionalPartMinusOne);

        return Mathf.SmoothStep
            (
                Mathf.SmoothStep
                (
                    Mathf.SmoothStep(dot000, dot100, fractionalPart.x),
                    Mathf.SmoothStep(dot010, dot110, fractionalPart.x),
                    fractionalPart.y
                ),
                Mathf.SmoothStep
                (
                    Mathf.SmoothStep(dot001, dot101, fractionalPart.x),
                    Mathf.SmoothStep(dot011, dot111, fractionalPart.x),
                    fractionalPart.y
                ),
                fractionalPart.z
            );
    }
    /*
    // samples the values selected from the permutation table based on the point's integer coordinates
    private static float sampleValue2D(int x, int y)
    {
        x &= hashmask;
        y &= hashmask;

        return permutationTable[(permutationTable[x] + y) & hashmask] * (1.0f / hashmask);
    }
    */
}
