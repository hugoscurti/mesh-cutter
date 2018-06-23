using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCutter
{

    public TempMesh PositiveMesh { get; private set; }
    public TempMesh NegativeMesh { get; private set; }

    private List<Vector3> addedPairs;

    private readonly Vector3[] intersectPair;
    private readonly Vector3[] tempTriangle;

    private Intersections intersect;

    public MeshCutter(int initialArraySize)
    {
        PositiveMesh = new TempMesh(initialArraySize);
        NegativeMesh = new TempMesh(initialArraySize);

        addedPairs = new List<Vector3>(initialArraySize);

        intersectPair = new Vector3[2];
        tempTriangle = new Vector3[3];

        intersect = new Intersections();
    }

    /// <summary>
    /// Slice a mesh by the slice plane.
    /// We assume the plane is already in the mesh's local coordinate frame
    /// Returns posMesh and negMesh, which are the resuling meshes on both sides of the plane 
    /// (posMesh on the same side as the plane's normal, negMesh on the opposite side)
    /// </summary>
    public bool SliceMesh(Mesh mesh, ref Plane slice)
    {
        var vertices = mesh.vertices;
        int triangleCount = mesh.triangles.Length;

        PositiveMesh.Clear();
        NegativeMesh.Clear();
        addedPairs.Clear();

        for (int i = 0; i < triangleCount; i += 3)
        {
            if (intersect.TrianglePlaneIntersect(mesh, i, ref slice, PositiveMesh, NegativeMesh, intersectPair))
                addedPairs.AddRange(intersectPair);
        }

        if (addedPairs.Count > 0)
        {
            FillBoundaryGeneral(addedPairs, PositiveMesh, NegativeMesh);
            return true;
        }
        else return false;
    }

    private void FillBoundaryGeneral(List<Vector3> added, TempMesh meshPositive, TempMesh meshNegative)
    {
        // 1. Reorder added so in order ot their occurence along the perimeter.
        //ReorderList(added);

        Vector3 center = FindCenter(added);

        //Create triangle for each edge to the center
        tempTriangle[2] = center;

        for (int i = 0; i < added.Count; i += 2)
        {
            // Add fronface triangle in meshPositive
            tempTriangle[0] = added[i];
            tempTriangle[1] = added[i + 1];

            meshPositive.AddTriangle(tempTriangle);

            // Add backface triangle in meshNegative
            tempTriangle[0] = added[i + 1];
            tempTriangle[1] = added[i];

            meshNegative.AddTriangle(tempTriangle);
        }
    }

    /// <summary>
    /// Find center of polygon by averaging vertices
    /// </summary>
    private static Vector3 FindCenter(List<Vector3> pairs)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < pairs.Count; i += 2)
        {
            center += pairs[i];
            count++;
        }

        return center / count;
    }

    /// <summary>
    /// Reorder a list of pairs of vectors, where the second vector of a pair matches the first vector of 
    /// </summary>
    private static void ReorderList(List<Vector3> pairs)
    {
        Vector3 tempFirst, tempSecond;
        int i = 0;
        while (i < pairs.Count)
        {
            for (int j = i + 2; j < pairs.Count; ++j)
            {
                if (pairs[j] == pairs[i + 1] && j != i + 2)
                {
                    // Put j at i+2
                    tempFirst = pairs[i + 2];
                    tempSecond = pairs[i + 3];
                    pairs[i + 2] = pairs[j];
                    pairs[i + 3] = pairs[j + 1];
                    pairs[j] = tempFirst;
                    pairs[j + 1] = tempSecond;
                    break;
                }
            }
        }
    }    
}

