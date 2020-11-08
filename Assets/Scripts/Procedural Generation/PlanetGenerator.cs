
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Water;

/* goal: defines how the planets' data is stored as well as an iterative
 * generation method to populate this data, which can then be handed off
 * to the DualContouringMeshGenerator to create meshes for all the valid
 * chunks that the renderer might need to render the surface of this
 * planet from any distance
 */

public struct MeshChunk
{

}

public struct PlanetData
{

}

public struct GenerationParameters
{
    public double size;
    public double chunkSize;
    public int verticesToEdge;
}

public class PlanetGenerator
{
    public static double desiredChunkSize = 50;
    public static double desiredVerticesToEdge = 64;



    public static GenerationParameters WithParameters(double s)
    {
        double chunkS = s;
        while (chunkS > desiredChunkSize)
        {
            chunkS *= 0.5;
        }

        return new GenerationParameters
        {
            size = s,
            chunkSize = chunkS,
            verticesToEdge = (int)((chunkS / desiredChunkSize) * desiredVerticesToEdge)
        };
    }
}
