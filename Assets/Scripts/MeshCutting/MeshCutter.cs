using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshCutter
{



    /// <summary>
    /// Slice a mesh by the slice plane.
    /// We assume the plane is already in the mesh's local coordinate frame
    /// Returns posMesh and negMesh, which are the resuling meshes on both sides of the plane 
    /// (posMesh on the same side as the plane's normal, negMesh on the opposite side)
    /// </summary>
    public static bool SliceMesh(Mesh mesh, Plane slice, out TempMesh posMesh, out TempMesh negMesh)
    {
        var vertices = mesh.vertices;
        int triangleCount = mesh.triangles.Length;

        posMesh = new TempMesh(vertices.Length);
        negMesh = new TempMesh(vertices.Length);

        List<Vector3> added = new List<Vector3>();
        Vector3[] temp;

        for (int i = 0; i < triangleCount; i += 3)
        {
            temp = Intersections.TrianglePlaneIntersect(mesh, i, ref slice, posMesh, negMesh);
            if (temp != null)
                added.AddRange(temp);
        }

        if (added.Count > 0)
        {
            FillBoundaryGeneral(added, posMesh, negMesh);
            return true;
        }
        else return false;
    }

    private static void FillBoundaryGeneral(List<Vector3> added, TempMesh meshPositive, TempMesh meshNegative)
    {
        // 1. Reorder added so in order ot their occurence along the perimeter.
        //ReorderList(added);

        Vector3 center = FindCenter(added);

        //Create triangle for each edge to the center
        Vector3[] tempTri = new Vector3[3];
        tempTri[2] = center;

        for (int i = 0; i < added.Count; i += 2)
        {
            // Add fronface triangle in meshPositive
            tempTri[0] = added[i];
            tempTri[1] = added[i + 1];

            meshPositive.AddTriangle(tempTri);

            // Add backface triangle in meshNegative
            tempTri[0] = added[i + 1];
            tempTri[1] = added[i];

            meshNegative.AddTriangle(tempTri);
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

