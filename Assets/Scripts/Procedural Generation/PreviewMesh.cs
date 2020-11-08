using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PreviewMesh : MonoBehaviour
{
    public MeshFilter meshFilter;

    public Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        RenderSettings.fog = false;
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void CreateAndDisplayMesh()
    {
        //Func<Vector3, float> Source = pt => -NoisePlane(pt + offset, 10.0f);

        Func<Vector3, float> Source = pt => -Torus(pt + offset, new Vector2(25.0f, 10.0f));

        VoxelOctree octree = VoxelOctree.CreateBasedOnFunction(Source, Vector3.zero, Vector3.one * 100.0f, 5);

        Mesh mesh = MeshGenerator.CreateMeshFromVoxels(octree, Source, 5, 0);

        meshFilter.mesh = mesh;
    }

    float Torus(Vector3 point, Vector2 size)
    {
        Vector2 q = new Vector2(new Vector2(point.x, point.z).magnitude - size.x, point.y);
        return q.magnitude - size.y;
    }

    float NoisePlane(Vector3 point, float noiseHeight)
    {
        return point.y + NoiseGenerator.sampleNoise2D(new Vector2(point.x, point.z)*0.05f) * noiseHeight;
    }

}
