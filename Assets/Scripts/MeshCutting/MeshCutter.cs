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
            temp = TrianglePlaneIntersect(mesh, i, ref slice, posMesh, negMesh);
            if (temp != null)
                added.AddRange(temp);
        }

        if (added.Count > 0)
        {
            FillBoundary(added, posMesh, negMesh);
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Add vertices and faces to fill the empty face created by the slice
    /// </summary>
    private static void FillBoundary(List<Vector3> added, TempMesh meshPositive, TempMesh meshNegative)
    {
        //TODO: Change method to handle all type of shape

        List<Vector3> triangles = new List<Vector3>();
        List<Vector3> centerEdges = new List<Vector3>();

        Vector3[] tempTri = new Vector3[3];

        HashSet<int> processed = new HashSet<int>();
        int prev, next;

        // Create triangles from corner edges
        // TODO: handle cases where number of edges is different than 16
        for (int i = 0; i < added.Count - 2; i += 2)
        {
            if (processed.Contains(i)) continue;
            for (int j = i + 2; j < added.Count; j += 2)
            {
                if (processed.Contains(j)) continue;
                if (added[i] == added[j + 1])
                {
                    prev = j;
                    next = i;
                }
                else if (added[i + 1] == added[j])
                {
                    prev = i;
                    next = j;
                }
                else continue;

                if (Vector3.Angle(added[prev + 1] - added[prev], added[next + 1] - added[next]) < 0.1) continue;

                // Form a triangle
                tempTri[0] = added[prev];
                tempTri[1] = added[prev + 1];
                tempTri[2] = added[next + 1];

                triangles.AddRange(tempTri);
                meshPositive.AddTriangle(tempTri);

                // Add twin edge for center edges
                centerEdges.Add(tempTri[0]);
                centerEdges.Add(tempTri[2]);

                processed.Add(prev);
                processed.Add(next);
                break;
            }
        }

        processed.Clear();
        // Connect center edges
        for (int i = 0; i < centerEdges.Count - 2; i += 2)
        {
            if (processed.Contains(i)) continue;
            for (int j = i + 2; j < centerEdges.Count; j += 2)
            {
                if (processed.Contains(j)) continue;

                if (centerEdges[i] == centerEdges[j + 1])
                {
                    prev = j;
                    next = i;
                }
                else if (centerEdges[i + 1] == centerEdges[j])
                {
                    prev = i;
                    next = j;
                }
                else continue;

                tempTri[0] = centerEdges[prev];
                tempTri[1] = centerEdges[prev + 1];
                tempTri[2] = centerEdges[next + 1];

                triangles.AddRange(tempTri);
                meshPositive.AddTriangle(tempTri);

                processed.Add(prev);
                processed.Add(next);
                break;
            }
        }

        //Put all reversed triangles in mesh negative
        for (int i = 0; i < triangles.Count; i += 3)
        {
            tempTri[0] = triangles[i + 2];
            tempTri[1] = triangles[i + 1];
            tempTri[2] = triangles[i];
            meshNegative.AddTriangle(tempTri);
        }
    }


    /*
     * Small diagram for reference :)
     *       |      |  /|
     *       |      | / |P1       
     *       |      |/  |         
     *       |    I1|   |
     *       |     /|   |
     *      y|    / |   |
     *       | P0/__|___|P2
     *       |      |I2
     *       |      |
     *       |___________________
     */
    private static Vector3[] TrianglePlaneIntersect(Mesh mesh, int triangleIdx, ref Plane plane, TempMesh posMesh, TempMesh negMesh)
    {
        Vector3[] p = {
            mesh.vertices[mesh.triangles[triangleIdx]],
            mesh.vertices[mesh.triangles[triangleIdx + 1]],
            mesh.vertices[mesh.triangles[triangleIdx + 2]]
        };

        bool[] positive = {
            plane.GetDistanceToPoint(p[0]) >= 0,
            plane.GetDistanceToPoint(p[1]) >= 0,
            plane.GetDistanceToPoint(p[2]) >= 0 };

        if (positive[0] == positive[1] && positive[1] == positive[2])
        {
            // All points are on the same side. No intersection
            // Add them to either positive or negative mesh
            (positive[0] ? posMesh : negMesh).AddTriangle(p);
            return null;
        }

        // Find lonely point
        int lonelyPoint = 0;
        if (positive[0] != positive[1])
            lonelyPoint = positive[0] != positive[2] ? 0 : 1;
        else
            lonelyPoint = 2;

        // Set previous point in relation to front face order
        int prevPoint = lonelyPoint - 1;
        if (prevPoint == -1) prevPoint = 2;
        // Set next point in relation to front face order
        int nextPoint = lonelyPoint + 1;
        if (nextPoint == 3) nextPoint = 0;


        // Get the 2 intersection points
        Vector3 newPointPrev = Intersect(ref plane, p[lonelyPoint], p[prevPoint]);
        Vector3 newPointNext = Intersect(ref plane, p[lonelyPoint], p[nextPoint]);

        Vector3[] newFace = { newPointPrev, p[lonelyPoint], newPointNext };

        //Set the new triangles and store them in respective tempmeshes
        (positive[lonelyPoint] ? posMesh : negMesh).AddTriangle(newFace);

        newFace[0] = p[prevPoint];
        newFace[1] = newPointPrev;
        newFace[2] = p[nextPoint];
        (positive[prevPoint] ? posMesh : negMesh).AddTriangle(newFace);

        newFace[0] = p[nextPoint];
        newFace[1] = newPointPrev;
        newFace[2] = newPointNext;
        (positive[prevPoint] ? posMesh : negMesh).AddTriangle(newFace);

        // We return the edge that will be in the correct orientation for the positive side mesh
        if (positive[lonelyPoint])
            return new Vector3[] { newPointPrev, newPointNext };
        else
            return new Vector3[] { newPointNext, newPointPrev };
    }


    private static Vector3 Intersect(ref Plane plane, Vector3 first, Vector3 second)
    {
        Ray edgeRay = new Ray(first, (second - first).normalized);
        float dist;

        plane.Raycast(edgeRay, out dist);
        return edgeRay.GetPoint(dist);
    }
}

