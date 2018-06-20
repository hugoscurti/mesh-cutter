using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSlice : MonoBehaviour {

    bool dragging;
    Vector3 start;
    Vector3 end;

    LineRenderer line;

    public GameObject plane;
    public GameObject SlicedPrefab;
    public Transform ObjectContainer;

    public float separation;

    Ray mouseRay;
    readonly float distanceFromNearPlane = 2;

    #region Utility Functions

    void DrawPlane(Vector3 normalVec)
    {
        Quaternion rotate = Quaternion.FromToRotation(Vector3.up, normalVec);

        plane.transform.localRotation = rotate;
        plane.transform.position = (end + start) / 2;
        plane.SetActive(true);
    }

    Vector3 GetMousePosOnCamera()
    {
        var cam = Camera.main;
        mouseRay = cam.ScreenPointToRay(Input.mousePosition);
        return mouseRay.GetPoint(cam.nearClipPlane + distanceFromNearPlane);
    }

    #endregion

    // Use this for initialization
    void Start () {
        dragging = false;

        line = GetComponent<LineRenderer>();
	}

    // Update is called once per frame
    void Update() {
        if (!dragging && Input.GetMouseButtonDown(0))
        {
            start = GetMousePosOnCamera();
            line.SetPosition(0, start);
            dragging = true;
        } 

        if (dragging)
        {
            line.SetPosition(1, GetMousePosOnCamera());
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            // Finished dragging. We draw the line segment
            end = GetMousePosOnCamera();
            line.SetPosition(1, end);
            dragging = false;

            //var depthAxis = Camera.main.transform.forward;
            var depthAxis = mouseRay.direction.normalized;

            var planeTangent = end - start;
            var normalVec = Vector3.Cross(depthAxis, planeTangent);

            DrawPlane(normalVec);
            SliceObjects(start, normalVec);
        }
    }

    void SliceObjects(Vector3 point, Vector3 normal)
    {
        Plane slicePlane = new Plane();
        var toSlice = GameObject.FindGameObjectsWithTag("Sliceable");
        GameObject obj;
        for (int i = 0; i < toSlice.Length; ++i)
        {
            obj = toSlice[i];

            //Convert plane in object's local frame
            slicePlane.SetNormalAndPosition(
                obj.transform.InverseTransformVector(normal),
                obj.transform.InverseTransformPoint(point));

            SliceObject(ref slicePlane, obj);
        }
    }

    void SliceObject(ref Plane slicePlane, GameObject obj)
    {
        var mesh = obj.GetComponent<MeshFilter>().mesh;

        TempMesh posMesh, negMesh;

        if (!MeshCutter.SliceMesh(mesh, slicePlane, out posMesh, out negMesh))
        {
            // If we didn't slice the object then no need to separate it into 2 objects
            Debug.Log("Didn't slice");
            return;
        }

        // TODO: Update center of mass

        // Put the bigger mesh in the original object
        bool posBigger = posMesh.surfacearea > negMesh.surfacearea;
        ReplaceMesh(mesh, (posBigger ? posMesh : negMesh), obj.GetComponent<MeshCollider>());

        // Create new Sliced object with the other mesh
        GameObject newObject = Instantiate(SlicedPrefab, ObjectContainer);
        newObject.transform.SetPositionAndRotation(obj.transform.position, obj.transform.rotation);
        var newObjMesh = newObject.GetComponent<MeshFilter>().mesh;
        ReplaceMesh(newObjMesh, (posBigger ? negMesh : posMesh), newObject.GetComponent<MeshCollider>());

        Transform posTransform, negTransform;
        if (posBigger)
        {
            posTransform = obj.transform;
            negTransform = newObject.transform;
        } else
        {
            posTransform = newObject.transform;
            negTransform = obj.transform;
        }

        // Separate meshes 
        SeparateMeshes(posTransform, negTransform, slicePlane.normal);
    }


    /// <summary>
    /// Replace the mesh with tempMesh.
    /// </summary>
    void ReplaceMesh(Mesh mesh, TempMesh tempMesh, MeshCollider collider = null)
    {
        mesh.Clear();
        mesh.vertices = tempMesh.vertices.ToArray();
        mesh.triangles = tempMesh.triangles.ToArray();
        mesh.normals = tempMesh.normals.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        if (collider != null)
        {
            collider.sharedMesh = mesh;
            collider.convex = true;
        }
    }

    void SeparateMeshes(Transform posTransform, Transform negTransform, Vector3 normal)
    {
        Vector3 separationVec = normal * separation;
        posTransform.position += separationVec;
        negTransform.position -= separationVec;
    }
}
