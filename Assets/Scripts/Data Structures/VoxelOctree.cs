using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using TMPro.EditorUtilities;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem.Controls;

/* hopefully eventually this class will be able to do CSG operations and that sort of thing

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


/* Testing with this data structure

- This is mostly supposed to just be data to conviniently feed the Dual Contouring algorithm 
the data it needs to create meshes from a function. The structure is generated once from a
function, and then Dual Contouring gets run a bunch of times on this structure over different
sections, at different traversal starting and ending depths depending on LOD level. Because of
this, it's going to be loaded in memory basically the whole time the player is on a planet. Also,
this is obviously going to be unique for each planet, and there's probably gonna be a lot of them.
So, making sure this is as lightweight as possible, and doing edits, etc is as fast as possible is
high priority. Maybe we'll end up doing chunks of this to a planet, even probably, but even then
all of these things are just as important.

- Node Size

DC must sample the function many additional times (in relatively unique locations) to generate the
initial meshes. 


*/

public class VoxelOctree
{
    public static Vector3Int[] octantOrder =
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 1),
        new Vector3Int(1, 1, 1)
    };

    public int maxDepth;
    public int freeNode;
    public Bounds rootAABB;

    public List<CellData> cells;
    public List<Node> nodes;

    const int minDepth = 3;

    public static VoxelOctree CreateBasedOnFunction(Func<Vector3, float> Source, Vector3 center, Vector3 size, int maximumDepth)
    {
        int numSamplesToEdge(int d)
        {
            return 1 << d;
        }

        Dictionary<Vector3Int, float> samples = new Dictionary<Vector3Int, float>();

        VoxelOctree octree = new VoxelOctree(center, size, maximumDepth);
        octree.nodes.Add(new Node());

        Vector3 min = octree.rootAABB.min;
        int n = numSamplesToEdge(maximumDepth);
        Vector3 leafSize = (size / n);

        Populate(0, Vector3Int.zero, 0, 0, TestCell(Vector3Int.zero, 0));

        return octree;

        byte TestCell(Vector3Int cellPosition, int depth)
        {
            int s = numSamplesToEdge(maximumDepth - depth); 
            float[] sampleValues = new float[8];

            for (int i = 0; i < 8; i++)
            {
                Vector3Int samplePos = cellPosition + s * octantOrder[i];

                if (!samples.TryGetValue(samplePos, out sampleValues[i]))
                {
                    Vector3 pos = min + Vector3.Scale(leafSize, samplePos);
                    float val = Source(pos);
                    samples.Add(samplePos, val);
                    sampleValues[i] = val;
                }
            }

            byte sign = 0;

            for (int i = 0; i < 8; i++)
            {
                sign <<= 1;
                if (sampleValues[i] < 0.0)
                    sign += 1;
            }
            return sign;
        }

        void Populate(int cell, Vector3Int cellPosition, int depth, int octant, byte sign)
        {
            if (depth > minDepth && sign == 0) // this cell is completely outside the solid and does not need subdividing further
            {
                return;
            }
            else if (depth > minDepth && sign == 255) // this cell is completely inside the solid and does not need subdividing further
            {
                octree.nodes[cell] = Node.Leaf(octant, -1, Byte.MaxValue);
            }
            else  // this cell contains both empty space and part of the solid --
            {
                if (depth < maximumDepth) // so divide it into 8 children
                {
                    byte branchFilled = 0;
                    int children = 0;
                    int firstChild = octree.nodes.Count;

                    byte[] signs = new byte[8];

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int testPosition = cellPosition + (numSamplesToEdge(maximumDepth - depth) / 2) * octantOrder[i];
                        signs[i] = TestCell(testPosition, depth + 1);

                        branchFilled <<= 1;

                        if (signs[i] > 0)
                        {
                            octree.nodes.Add(new Node());
                            children++;
                            branchFilled++;
                        }
                    }

                    octree.nodes[cell] = Node.Branch(octant, firstChild, children, branchFilled);

                    int c = 0;

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int testPosition = cellPosition + (numSamplesToEdge(maximumDepth - depth) / 2) * octantOrder[i];
                        if ((branchFilled >> (7 - i)) % 2 == 1)
                        {
                            Populate(firstChild + c, testPosition, depth + 1, i, signs[i]);
                            c++;
                        }
                    }
                }
                else // but we are at the maximum allowed detail level, so put a vertex in this cell
                {
                    octree.nodes[cell] = Node.Leaf(octant, octree.cells.Count, sign);

                    CellData cellData = new CellData
                    {
                        cellMin = min + Vector3.Scale(leafSize, cellPosition),
                        cellSize = leafSize.x
                    };
                    octree.cells.Add(cellData);
                }
            }
        }
    }

    public VoxelOctree(Vector3 center, Vector3 size, int depth)
    {
        maxDepth = depth;
        freeNode = -1;
        rootAABB = new Bounds(center, size);
        nodes = new List<Node>();
        cells = new List<CellData>();
    }
    
    public struct Node
    {                                       //              branch or root                |          leaf
        private int firstChildOrMaterial;   // index of this node's first child           | index into the vertex list
        public sbyte numChildren;           // number of children this node has           | contains 0
        public byte filled;                 // which corners contain nodes                | which corners lie inside the function
        public byte octant;                 // which child is this in octantOrder (0 - 8) | same as branch/root

        public static Node Branch(int oct, int firstChild, int nChildren, byte whichChildren)
        {
            return new Node
            {
                firstChildOrMaterial = firstChild,
                numChildren = (sbyte)nChildren,
                octant = (byte)oct,
                filled = whichChildren
            };
        }

        public static Node Leaf(int oct, int material, byte value)
        {
            return new Node
            {
                firstChildOrMaterial = material,
                numChildren = 0,
                filled = value,
                octant = (byte)oct
            };
        }

        public int firstChild { 
            get { return firstChildOrMaterial; } 
            set { firstChildOrMaterial = value; }
        }

        public int material
        {
            get { return firstChildOrMaterial; }
            set { firstChildOrMaterial = value; }
        }

        public bool isCornerFilled(int octant)
        {
            return (filled >> (7 - octant)) % 2 == 1;
        }

        public int getIndexOfChild(int childOctant)
        {
            // theres probably a better way to do this
            int childrenBeforeDesired = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((filled >> (7 - i)) % 2 == 1)
                {
                    if (childOctant == i)
                    {
                        return childrenBeforeDesired + firstChildOrMaterial;
                    }
                    childrenBeforeDesired++;
                }
            }
            return -1;
        }

        public bool isLeaf { get { return numChildren == 0; } }
    }

    public struct CellData
    {
        public Vector3 cellMin;
        public float cellSize;
    }

    public void PrintOctree()
    {
        StringBuilder sb = new StringBuilder();

        Traverse(0, 0);

        void Traverse(int depth, int node)
        {
            sb.Append("\nNode index: ");
            sb.Append(node);
            sb.Append("\nDepth: ");
            sb.Append(depth);

            if (nodes[node].isLeaf)
            {
                sb.Append("\nFilled:");
                for (int i = 0; i < 8; i++)
                {
                    sb.Append(nodes[node].isCornerFilled(i) ? 1 : 0);
                }
            }
            else
            {
                sb.Append("\nHas ");
                sb.Append(nodes[node].numChildren);
                sb.Append(" children:");

                List<int> children = new List<int>(); 

                for (int i = 0; i < 8; i++)
                {
                    int ind;
                    if ((ind = nodes[node].getIndexOfChild(i)) != -1)
                    {
                        children.Add(ind);
                        sb.Append(ind);
                        sb.Append(" ");
                    }
                }

                foreach (int child in children) {
                    Traverse(depth + 1, child);
                }
            }
        }

        UnityEngine.Debug.Log(sb.ToString());
    }
}