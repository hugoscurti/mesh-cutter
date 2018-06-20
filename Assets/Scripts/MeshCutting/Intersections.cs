using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Intersections
{
    /// <summary>
    /// Find intersection between a plane and a line segment defined by vectors first and second.
    /// </summary>
    public static Vector3 Intersect(ref Plane plane, Vector3 first, Vector3 second)
    {
        Ray edgeRay = new Ray(first, (second - first).normalized);
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
    public static Vector3[] TrianglePlaneIntersect(Mesh mesh, int triangleIdx, ref Plane plane, TempMesh posMesh, TempMesh negMesh)
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



}
