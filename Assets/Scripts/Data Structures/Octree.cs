using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

/* order for nodes in a full cell:

       6-------7    
      /|      /|    Y
     / |     / |    |   
    4-------5  |    |  / Z
    |  2----|--3    | /
    | /     | /       ----- X
    0-------1

0 - (0, 0, 0)
1 - (1, 0, 0)
2 - (0, 0, 1)
3 - (1, 0, 1)
4 - (0, 1, 0)
5 - (1, 1, 0)
6 - (0, 1, 1)
7 - (1, 1, 1)   
*/
public class Octree<T>
{
    public static Vector3[] octantOrder =
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0),
        new Vector3(0, 1, 1),
        new Vector3(1, 1, 1)
    };

    public int maxDepth;
    public int freeNode;
    public Bounds rootAABB;
    
    public List<Node> nodes;
    public FreeList<T> elements;

    public Octree(Vector3 center, Vector3 size, int depth) {
        maxDepth = depth;
        freeNode = -1;
        rootAABB = new Bounds(center, size);
        nodes = new List<Node>();
        elements = new FreeList<T>();
    }

    public struct Node
    {
        public int child;
        /*
        if isLeaf is false, this node is either the root or a branch, and nodes[child] through
        nodes[child + 7] contain the 8 children of this node
        if isLeaf is true, this node is a leaf, and elements[child] is the element contained
        by this leaf.
        */
        public bool isLeaf;
    }

    public void DrawDebugCells()
    {
        DrawDebugCells(Color.white, Color.black, Color.green);
    }

    public void DrawDebugCells(Color empty, Color filled, Color contour)
    {
        DrawDebugCellsRecursive(0, rootAABB);

        void DrawDebugCellsRecursive(int node, Bounds aabb)
        {
            Color cellColor;
            var n = nodes[node];
            if (n.isLeaf)
            {
                if (n.child == MeshGenerator.LEAF_EMPTY)
                {
                    cellColor = empty;
                }
                else if (n.child == MeshGenerator.LEAF_FILLED)
                {
                    cellColor = filled;
                }
                else
                {
                    cellColor = contour;
                }
                for (int i = 0; i < 12; i++)
                {
                    Vector3 pt1 = aabb.min + Vector3.Scale(aabb.size, octantOrder[MeshGenerator.edgeTraversalOrder[i,0]]);
                    Vector3 pt2 = aabb.min + Vector3.Scale(aabb.size, octantOrder[MeshGenerator.edgeTraversalOrder[i,1]]);

                    UnityEngine.Debug.DrawLine(pt1, pt2, cellColor, 1.0f);
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    Bounds childAABB = new Bounds((aabb.min + aabb.extents * 0.5f) + Vector3.Scale(octantOrder[i], aabb.extents), aabb.extents);

                    DrawDebugCellsRecursive(n.child + i, childAABB);
                }
            }
        }
    }

    private void RecursiveToString(StringBuilder sb, int node, int depth)
    {
        var n = nodes[node];
        for (int i = 0; i < depth; i++)
        {
            sb.Append("\t");
        }
        if (n.isLeaf)
        {
            if (n.child == MeshGenerator.LEAF_EMPTY)
            {
                sb.Append("Leaf - Empty\n");
            }
            else if (n.child == MeshGenerator.LEAF_FILLED)
            {
                sb.Append("Leaf - Filled\n");
            }
            else
            {
                sb.Append("Leaf - Contour: " + elements[n.child].ToString() + "\n");
            }
        }
        else
        {
            sb.Append(node != 0 ? "Branch\n" : "Root\n");
            for (int i = 0; i < 8; i++)
            {
                RecursiveToString(sb, n.child + i, depth + 1);
            }
        }
    }
    override public string ToString()
    {
        StringBuilder sb = new StringBuilder();
        RecursiveToString(sb, 0, 0);
        return sb.ToString();
    }
}