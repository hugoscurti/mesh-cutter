using System.Collections.Generic;
using UnityEngine;

public class Intersections
{
    #region Static functions

    /// <summary>
    /// Based on https://gdbooks.gitbooks.io/3dcollisions/content/Chapter2/static_aabb_plane.html
    /// </summary>
    public static bool BoundPlaneIntersect(Mesh mesh, ref Plane plane)
    {
        // Compute projection interval radius
        float r = mesh.bounds.extents.x * Mathf.Abs(plane.normal.x) +
            mesh.bounds.extents.y * Mathf.Abs(plane.normal.y) +
            mesh.bounds.extents.z * Mathf.Abs(plane.normal.z);

        // Compute distance of box center from plane
        float s = Vector3.Dot(plane.normal, mesh.bounds.center) - (-plane.distance);

        // Intersection occurs when distance s falls within [-r,+r] interval
        return Mathf.Abs(s) <= r;
    }

    #endregion

    // Initialize fixed arrays so that we don't initialize them every time we call TrianglePlaneIntersect
    private readonly Vector3[] v;
    private readonly int[] t;
    private readonly bool[] positive;

    // Used in intersect method
    private Ray edgeRay;

    public Intersections()
    {
        v = new Vector3[3];
        t = new int[3];
        positive = new bool[3];
    }

    /// <summary>
    /// Find intersection between a plane and a line segment defined by vectors first and second.
    /// </summary>
    public Vector3 Intersect(Plane plane, Vector3 first, Vector3 second)
    {
        edgeRay.origin = first;
        edgeRay.direction = (second - first).normalized;
        float dist;

        plane.Raycast(edgeRay, out dist);
        return edgeRay.GetPoint(dist);
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

    public bool TrianglePlaneIntersect(List<Vector3> vertices, List<int> triangles, int startIdx, ref Plane plane, TempMesh posMesh, TempMesh negMesh, Vector3[] intersectVectors)
    {
        int i;
        // Store triangle indices
        for(i = 0; i < 3; ++i)
            t[i] = triangles[startIdx + i];

        // Store associated vertices
        for (i = 0; i < 3; ++i)
            v[i] = vertices[t[i]];

        // Store wether the vertex is on positive mesh
        posMesh.ContainsKeys(triangles, startIdx, positive);

        // If they're all on the same side, don't do intersection
        if (positive[0] == positive[1] && positive[1] == positive[2])
        {
            // All points are on the same side. No intersection
            // Add them to either positive or negative mesh
            (positive[0] ? posMesh : negMesh).AddOgTriangle(t);
            return false;
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
        Vector3 newPointPrev = Intersect(plane, v[lonelyPoint], v[prevPoint]);
        Vector3 newPointNext = Intersect(plane, v[lonelyPoint], v[nextPoint]);

        //Set the new triangles and store them in respective tempmeshes
        (positive[lonelyPoint] ? posMesh : negMesh).AddSlicedTriangle(t[lonelyPoint], newPointNext, newPointPrev);

        (positive[prevPoint] ? posMesh : negMesh).AddSlicedTriangle(t[prevPoint], newPointPrev, t[nextPoint]);

        (positive[prevPoint] ? posMesh : negMesh).AddSlicedTriangle(t[nextPoint], newPointPrev, newPointNext);

        // We return the edge that will be in the correct orientation for the positive side mesh
        if (positive[lonelyPoint])
        {
            intersectVectors[0] = newPointPrev;
            intersectVectors[1] = newPointNext;
        } else
        {
            intersectVectors[0] = newPointNext;
            intersectVectors[1] = newPointPrev;
        }
        return true;
    }



}
