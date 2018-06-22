using System;
using System.Collections.Generic;
using UnityEngine;


public class TempMesh
{
    public List<Vector3> vertices;
    public List<Vector3> normals;
    public List<int> triangles;

    public float surfacearea;

    public TempMesh(int vertexCapacity)
    {
        vertices = new List<Vector3>(vertexCapacity);
        normals = new List<Vector3>(vertexCapacity);
        triangles = new List<int>(vertexCapacity * 3);

        surfacearea = 0;
    }

    public void Clear()
    {
        vertices.Clear();
        normals.Clear();
        triangles.Clear();

        surfacearea = 0;
    }

    /// <summary>
    /// Add triangle to mesh by looking if points are not already in vertices array and updating triangle indices accordingly.
    /// We expect points to be of size 3
    /// </summary>
    public void AddTriangle(Vector3[] points)
    {
        // Compute normal
        Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[1]).normalized;

        //Compute triangle area
        surfacearea += GetTriangleArea(points);

        int idx;
        for (int i = 0; i < 3; ++i)
        {
            idx = vertices.IndexOf(points[i]);
            if (idx == -1 || normals[idx] != normal)
            {
                vertices.Add(points[i]);
                normals.Add(normal);
                idx = vertices.Count - 1;
            }

            triangles.Add(idx);
        }
    }

    private float GetTriangleArea(Vector3[] p)
    {
        var va = p[2] - p[0];
        var vb = p[1] - p[0];
        float a = va.magnitude;
        float b = vb.magnitude;
        float gamma = Mathf.Deg2Rad * Vector3.Angle(vb, va);

        return a * b * Mathf.Sin(gamma) / 2;
    }
}

