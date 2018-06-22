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
    private readonly Vector3[] p;
    private readonly bool[] positive;
    private readonly Vector3[] newFaces;

    // Used in intersect method
    private Ray edgeRay;

    public Intersections()
    {
        p = new Vector3[3];
        positive = new bool[3];
        newFaces = new Vector3[3];
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

    public bool TrianglePlaneIntersect(Mesh mesh, int triangleIdx, ref Plane plane, TempMesh posMesh, TempMesh negMesh, Vector3[] intersectVectors)
    {
        p[0] = mesh.vertices[mesh.triangles[triangleIdx]];
        p[1] = mesh.vertices[mesh.triangles[triangleIdx + 1]];
        p[2] = mesh.vertices[mesh.triangles[triangleIdx + 2]];

        positive[0] = plane.GetDistanceToPoint(p[0]) >= 0;
        positive[1] = plane.GetDistanceToPoint(p[1]) >= 0;
        positive[2] = plane.GetDistanceToPoint(p[2]) >= 0;

        if (positive[0] == positive[1] && positive[1] == positive[2])
        {
            // All points are on the same side. No intersection
            // Add them to either positive or negative mesh
            (positive[0] ? posMesh : negMesh).AddTriangle(p);
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
        Vector3 newPointPrev = Intersect(plane, p[lonelyPoint], p[prevPoint]);
        Vector3 newPointNext = Intersect(plane, p[lonelyPoint], p[nextPoint]);

        newFaces[0] = newPointPrev;
        newFaces[1] = p[lonelyPoint];
        newFaces[2] = newPointNext;

        //Set the new triangles and store them in respective tempmeshes
        (positive[lonelyPoint] ? posMesh : negMesh).AddTriangle(newFaces);

        newFaces[0] = p[prevPoint];
        newFaces[1] = newPointPrev;
        newFaces[2] = p[nextPoint];
        (positive[prevPoint] ? posMesh : negMesh).AddTriangle(newFaces);

        newFaces[0] = p[nextPoint];
        newFaces[1] = newPointPrev;
        newFaces[2] = newPointNext;
        (positive[prevPoint] ? posMesh : negMesh).AddTriangle(newFaces);

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
