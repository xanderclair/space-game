using JetBrains.Annotations;
using Packages.Rider.Editor.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Serialization.Configuration;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager.UI;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityStandardAssets.Effects;

public class MeshGenerator
{
    private static float epsilon = 0.0001f;
    public static int LEAF_EMPTY = -1;
    public static int LEAF_FILLED = -2;
    private static int[] quad = { 0, 1, 2, 2, 3, 0 };
    public static int[,] edgeTraversalOrder =
    {
        { 0, 1 }, // four pairs of corners for testing X axis
        { 2, 3 },
        { 6, 7 },
        { 4, 5 },

        { 0, 4 }, // four pairs of corners for testing Y axis
        { 1, 5 },
        { 3, 7 },
        { 2, 6 },

        { 0, 2 }, // four pairs of corners for testing Z axis
        { 1, 3 },
        { 5, 7 },
        { 4, 6 }
    };

    public static int[,] faceTraversalOrder =
    {
        { 0, 1, 3, 2 },
        { 4, 5, 7, 6 },
        { 0, 4, 5, 1 },
        { 2, 6, 7, 3 },
        { 0, 2, 6, 4 },
        { 1, 3, 7, 5 }
    };


    public struct ContourElement
    {
        public ContourElement(Vector3 pos, bool[] axes)
        {
            vertex = pos;
            signChange = axes;
        }
        public Vector3 vertex;
        public bool[] signChange;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(vertex.ToString());
            sb.Append(", Sign Change: ");
            for (int i = 0; i < signChange.Length; i++)
            {
                sb.Append((signChange[i] ? " -" : " +") + "OXYZ"[i]);
            }
            return sb.ToString();
        }
    }

    struct OctreeVertexData
    {
        public Vector3[] positions;
        public int[] nodeIndices;
    }

    /*
    public static Mesh GenerateMesh(Vector3 center, Vector3 size, Func<Vector3, float> Source)
    {
        Octree<ContourElement> octree = new Octree<ContourElement>(center, size, maxDepth);
        octree.nodes.Add(new Octree<ContourElement>.Node());


        //PopulateOctreeBasedOnFunctionDC(Source, octree, 0, 0, octree.rootAABB);

        List<uint> faces = FindFacesInOctree(octree);

        List<int> signedFaces = new List<int>();

        foreach (uint face in faces)
        {
            signedFaces.Add((int)face);
        }

        Vector3[] vertices = UnpackVertices(octree);

        Vector3[] normals = new Vector3[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            normals[i] = GradientOf(vertices[i], Source);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = signedFaces.ToArray();
        mesh.normals = normals;
        return mesh;
    }
    */
    static int NumSamplesToEdge(int d)
    {
        return (1 << d) + 1; // 2^d + 1
    }

    /* Traverses through a VoxelOctree and creates a Mesh in two steps:
     * 
     * 1. Find the connected cells of the octree and populate the index list - done?
     * 
     * 2. Traverse and position each vertex within the cell
     */
    public static Mesh CreateMeshFromVoxels(VoxelOctree fromOctree, Func<Vector3, float> Source, int traversalDepth, int startCell)
    {
        List<int> faceData = FindFacesInOctree(fromOctree, startCell, traversalDepth);

        OctreeVertexData vertexData = PositionVerticesInOctree(fromOctree, Source, fromOctree.maxDepth);
        
        int[] triangles = new int[faceData.Count];
        
        for (int i = 0; i < triangles.Length; i++)
        {
            for (int j = 0; j < vertexData.nodeIndices.Length; j++)
            {
                if (vertexData.nodeIndices[j] == faceData[i])
                {
                    triangles[i] = j;
                    break;
                }
            }
        }

        for (int i = 0; i < 12; i++)
        {
            Vector3 pt0 = fromOctree.rootAABB.min + Vector3.Scale(VoxelOctree.octantOrder[edgeTraversalOrder[i, 0]], fromOctree.rootAABB.size);
            Vector3 pt1 = fromOctree.rootAABB.min + Vector3.Scale(VoxelOctree.octantOrder[edgeTraversalOrder[i, 1]], fromOctree.rootAABB.size);
            UnityEngine.Debug.DrawLine(pt0, pt1, Color.black, 1.0f);        
        }

        Mesh mesh = new Mesh
        {
            vertices = vertexData.positions,
            triangles = triangles
        };

        mesh.RecalculateNormals();

        return mesh;
    }

    private static OctreeVertexData PositionVerticesInOctree(VoxelOctree fromOctree, Func<Vector3, float> Source, int traversalDepth)
    {
        Vector3 min = fromOctree.rootAABB.min;
        int n = NumSamplesToEdge(traversalDepth);
        Vector3 leafSize = (fromOctree.rootAABB.size / n);

        List<Vector3> vertices = new List<Vector3>();
        List<int> vertexNodeIndices = new List<int>();

        Traverse(0, 0);

        return new OctreeVertexData
        {
            positions = vertices.ToArray(),
            nodeIndices = vertexNodeIndices.ToArray()
        };

        void Traverse(int cell, int depth)
        {
            VoxelOctree.Node node = fromOctree.nodes[cell];

            if (node.isLeaf && node.filled != 255) // || depth > traversalDepth)
            {
                VoxelOctree.CellData data = fromOctree.cells[node.firstChild];
                Vector3 pos = PositionVertexInDCCell(Source, data.cellMin, data.cellSize, node);
                //UnityEngine.Debug.Log(pos + "");
                vertices.Add(pos);
                vertexNodeIndices.Add(cell);
                return;
            }
            else if (node.isLeaf) { return; }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    int childIndex;
                    if ((childIndex = node.getIndexOfChild(i)) != -1)
                    {
                        Traverse(childIndex, depth + 1);
                    }
                }
            }
        }
    }
    /*
    static void PopulateOctreeBasedOnFunctionDC(Func<Vector3, float> Source, float[,,] map, Octree<ContourElement> octree, int cellIndex, int depth, Bounds cell)
    {
        float[] samples = new float[8];
        Vector3 min = cell.min;
        Vector3 size = cell.size;
        // sample all 8 corner points of the cell (TODO: test caching these values, each unique point gets sampled like 4 times at least)
        for (int i = 0; i < 8; i++)
        {
            samples[i] = Source(min + Vector3.Scale(Octree<ContourElement>.octantOrder[i], size));
        }

        Octree<ContourElement>.Node node = octree.nodes[cellIndex];

        int sign = 0;
        for (int i = 0; i < 8; i++)
        {
            sign += (int)Mathf.Sign(samples[i]);
        }

        sign *= depth < minDepth ? 0 : 1;

        if (sign == 8) // this cell is completely outside the solid and does not need subdividing further
        {
            node.isLeaf = true;
            node.child = LEAF_EMPTY;
            octree.nodes[cellIndex] = node;
        }
        else if (sign == -8) // this cell is completely inside the solid and does not need subdividing further
        {
            node.isLeaf = true;
            node.child = LEAF_FILLED;
            octree.nodes[cellIndex] = node;
        }
        else  // this cell contains both empty space and part of the solid --
        {
            if (depth <= maxDepth) // so divide it into 8 children
            {
                node.isLeaf = false;
                node.child = octree.nodes.Count;
                octree.nodes[cellIndex] = node;

                Vector3 ctr = cell.center - (cell.extents / 2.0f);

                for (int i = 0; i < 8; i++)
                {
                    var newNode = new Octree<ContourElement>.Node();
                    octree.nodes.Add(newNode);
                }

                for (int i = 0; i < 8; i++)
                {
                    Bounds aabb = new Bounds(ctr + Vector3.Scale(Octree<ContourElement>.octantOrder[i], cell.extents), cell.extents);

                    //PopulateOctreeBasedOnFunctionDC(Source, octree, node.child + i, depth + 1, aabb);
                }
            }
            else // but we are at the maximum allowed detail level, so put a vertex in this cell
            {
                node.isLeaf = true;

                ContourElement contour = PositionVertexInDCCell(Source, samples, cell);

                node.child = octree.elements.Insert(contour);
                octree.nodes[cellIndex] = node;
            }
        }

    }
    */
    // recursively construct the actual mesh from the octree of vertices
    private static List<int> FindFacesInOctree(VoxelOctree octree, int startingCell, int traversalDepth)
    {
        /* these lookup tables just hold the variations between the different traversal routes when
         * looking through the octree for vertices to connect into faces.
         * 
         * o - offsets for looking at the children of nodes next to larger leaf nodes. depending on
         * the orientation of the four cells EdgeProc looks at, it needs to select the adjacent child
         * 
         * l - CellProc calls each traversal method twice for each level; the way the indexes are 
         * selected for the octree means going from the lesser valued variant axis side to the 
         * higher valued one can always be done by adding a constant, stored in this index
         * 
         * a - when deciding whether to draw a face, EdgeProc tests two points of the function in the
         * group of cells in question, varied along one axis depending on which orientation the four
         * cells are. as the variable names suggest, the test for the cells along the XZ plane involve
         * varying axis 2, or Y. then; 1 = X and 3 = Z
         */

        int[][] lookEdge = {
        //              |    o     | l| a|
             new int[] { 3, 2, 0, 1, 4, 2 }, // XZ
             new int[] { 5, 1, 0, 4, 2, 3 }, // XY
             new int[] { 6, 4, 0, 2, 1, 1 }, // ZY
        };

        // e, i - FaceProc must also call EdgeProc, but it has to use the correct lookEdge array for the
        // FaceProc call. the last two groups here show which children nodes to select for those calls,
        // as well as which lookEdge to use

        int[][] lookFace =
        {
        //              | o   |  l  |       e1      |       e2     |   i  |
             new int[] { 1, 0, 4, 2, -1,  0,  2, -3, -1, -5,  4,  0, 0, 1 }, // X
             new int[] { 4, 0, 2, 1, -4,  0,  1, -5, -4, -6,  2,  0, 1, 2 }, // Y
             new int[] { 2, 0, 4, 1, -2, -3,  1,  0, -2,  0,  4, -6, 0, 2 }, // Z
        };

        List<int> faces = new List<int>();

        CellProc(0, startingCell);

        return faces;

        void CellProc(int depth, int node)
        {
            VoxelOctree.Node currentNode = octree.nodes[node];

            if (currentNode.isLeaf || depth > traversalDepth) return;

            for (int i = 0; i < 8; i++)
            {
                int ind;
                if ((ind = currentNode.getIndexOfChild(i)) != -1)
                {
                    CellProc(depth + 1, ind);
                }
            }

            for (int i = 0; i < 12; i++)
            {
                int ind1, ind2;
                if ((ind1 = currentNode.getIndexOfChild(edgeTraversalOrder[i, 0])) != -1 &&
                    (ind2 = currentNode.getIndexOfChild(edgeTraversalOrder[i, 1])) != -1)
                {
                    FaceProc(depth + 1, ind1, ind2, lookFace[i / 4]);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                int ind1, ind2, ind3, ind4;
                if ((ind1 = currentNode.getIndexOfChild(faceTraversalOrder[i, 0])) != -1 &&
                    (ind2 = currentNode.getIndexOfChild(faceTraversalOrder[i, 1])) != -1 &&
                    (ind3 = currentNode.getIndexOfChild(faceTraversalOrder[i, 2])) != -1 &&
                    (ind4 = currentNode.getIndexOfChild(faceTraversalOrder[i, 3])) != -1)
                {
                    EdgeProc(depth + 1, ind1, ind2, ind3, ind4, lookEdge[i / 2]);
                }
            }
        }

        void FaceProc(int depth, int node0, int node1, int[] look)
        {
            VoxelOctree.Node[] n = { octree.nodes[node0], octree.nodes[node1] };

            if (n[0].isLeaf || n[1].isLeaf || depth > traversalDepth) return;

            int[,] nextFaces = new int[2, 4];

            for (int i = 0; i < 2; i++)
            {
                nextFaces[i, 0] = look[i];
                nextFaces[i, 1] = look[i] + look[2];
                nextFaces[i, 2] = look[i] + look[3];
                nextFaces[i, 3] = look[i] + look[2] + look[3];
            }

            for (int i = 0; i < 4; i++)
            {
                int ind1, ind2;
                if ((ind1 = n[0].getIndexOfChild(nextFaces[0, i])) != -1 &&
                    (ind2 = n[1].getIndexOfChild(nextFaces[1, i])) != -1)
                {
                    FaceProc(depth + 1, ind1, ind2, look);
                }
            }

            nextFaces = new int[4, 4];

            for (int i = 0; i < 4; i++)
            {
                int nextNode1 = Math.Sign(look[i + 4]) == -1 ? 0 : 1;
                int nextNode2 = Math.Sign(look[i + 8]) == -1 ? 0 : 1;

                nextFaces[i, 0] = n[nextNode1].getIndexOfChild(Math.Abs(look[i + 4]));
                nextFaces[i, 1] = n[nextNode1].getIndexOfChild(Math.Abs(look[i + 4]) + look[2]);
                nextFaces[i, 2] = n[nextNode2].getIndexOfChild(Math.Abs(look[i + 8]));
                nextFaces[i, 3] = n[nextNode2].getIndexOfChild(Math.Abs(look[i + 8]) + look[3]);
            }

            for (int i = 0; i < 4; i++)
            {
                if (nextFaces[0, i] != -1 &&
                    nextFaces[1, i] != -1 &&
                    nextFaces[2, i] != -1 &&
                    nextFaces[3, i] != -1)
                {
                    EdgeProc(depth + 1, nextFaces[0, i], nextFaces[1, i], nextFaces[2, i], nextFaces[3, i], lookEdge[look[12 + i / 2]]);
                }
            }

        }

        void EdgeProc(int depth, int node0, int node1, int node2, int node3, int[] look)
        {
            VoxelOctree.Node[] n = { octree.nodes[node0], octree.nodes[node1], octree.nodes[node2], octree.nodes[node3] };

            if (/*depth > traversalDepth ||*/ (n[0].isLeaf && n[1].isLeaf && n[2].isLeaf && n[3].isLeaf))
            {
                foreach (VoxelOctree.Node node in n)
                {
                    if (node.filled == 255)
                    {
                        // one of the leaves does not contain a contour
                        return;
                    }
                }
                // test for a sign change along the edge shared by these four cells
                int[] signChangeTests = { 1, 4, 2 };
                bool flipFace;
                /*
                StringBuilder sb = new StringBuilder();
                sb.Append(look[0]);
                sb.Append(": ");
                for (int i = 0; i < 8; i++)
                {
                    sb.Append(n[2].isCornerFilled(i) ? 1 : 0);
                }
                sb.Append("\n");
                sb.Append(octree.cells[n[2].firstChild].cellMin);
                sb.Append(depth);
                UnityEngine.Debug.Log(sb.ToString());
                */

                if (n[2].isCornerFilled(0) != (flipFace = n[2].isCornerFilled(signChangeTests[look[5] - 1])))
                {
                    List<int> face = new List<int> { node0, node1, node2, node3 };

                    if (flipFace) face.Reverse();

                    faces.Add(face[0]);
                    faces.Add(face[1]);
                    faces.Add(face[2]);
                    faces.Add(face[2]);
                    faces.Add(face[3]);
                    faces.Add(face[0]);
                }
            }
            else
            {
                // one or more of the children of this node are subdivided further, so we need to recurse twice.
                int[] nextNodes = { node0, node1, node2, node3, node0, node1, node2, node3 };

                for (int i = 0; i < 4; i++)
                {
                    if (!n[i].isLeaf)
                    {
                        nextNodes[i] = n[i].getIndexOfChild(look[i]);
                        nextNodes[i + 4] = n[i].getIndexOfChild(look[i] + look[4]);
                    }
                }
                if (nextNodes[0] != -1 && nextNodes[1] != -1 && nextNodes[2] != -1 && nextNodes[3] != -1)
                {
                    EdgeProc(depth + 1, nextNodes[0], nextNodes[1], nextNodes[2], nextNodes[3], look);
                }
                if (nextNodes[4] != -1 && nextNodes[5] != -1 && nextNodes[6] != -1 && nextNodes[7] != -1)
                {
                    EdgeProc(depth + 1, nextNodes[4], nextNodes[5], nextNodes[6], nextNodes[7], look);
                }
            }
        }
    }

    static Vector3 GradientOf(Vector3 pt, Func<Vector3, float> Source)
    {
        float dx = Source(new Vector3(pt.x + epsilon, pt.y, pt.z)) - Source(new Vector3(pt.x - epsilon, pt.y, pt.z));
        float dy = Source(new Vector3(pt.x, pt.y + epsilon, pt.z)) - Source(new Vector3(pt.x, pt.y - epsilon, pt.z));
        float dz = Source(new Vector3(pt.x, pt.y, pt.z + epsilon)) - Source(new Vector3(pt.x, pt.y, pt.z - epsilon));

        return new Vector3(dx, dy, dz).normalized;
    }
    static Vector3 FindZeroAlongEdge(Func<Vector3, float> Source, Vector3 cellMin, float size, int edgeIndex)
    {
        Vector3 pt0 = cellMin + Vector3.Scale(VoxelOctree.octantOrder[edgeTraversalOrder[edgeIndex, 0]], new Vector3(size, size, size));
        Vector3 pt1 = cellMin + Vector3.Scale(VoxelOctree.octantOrder[edgeTraversalOrder[edgeIndex, 0]], new Vector3(size, size, size));

        
        float val0 = Source(pt0);
        float val1;

        int numSamples = 8;

        for (int i = 0; i < numSamples; i++)
        {
            Vector3 testPoint = (pt0 + pt1) * 0.5f;
            float val = Source(testPoint);
            if (Mathf.Sign(val) * Mathf.Sign(val0) > 0.0)
            {
                pt0 = testPoint;
                val0 = val;
            }
            else
            {
                pt1 = testPoint;
                val1 = val;
            }
        }
        

        return (pt0 + pt1) * 0.5f;
    }
    static Vector3 PositionVertexInDCCell(Func<Vector3, float> Source, Vector3 cellMin, float size, VoxelOctree.Node node)
    {
        List<Vector3> zeroesAlongCellEdges = new List<Vector3>();

        for (int i = 0; i < 12; i++)
        {
            if (node.isCornerFilled(edgeTraversalOrder[i, 0]) != node.isCornerFilled(edgeTraversalOrder[i, 1]))
            {
                zeroesAlongCellEdges.Add(FindZeroAlongEdge(Source, cellMin, size, i));
            }
        }
        // naive surface nets implementation for localization for now :(

        Vector3 v = Vector3.zero;
        foreach (Vector3 intersection in zeroesAlongCellEdges)
        {   
            v += intersection;
        }

        v /= (float)zeroesAlongCellEdges.Count;

        return v;// cellMin + 0.5f * new Vector3(size, size, size);
    }
}