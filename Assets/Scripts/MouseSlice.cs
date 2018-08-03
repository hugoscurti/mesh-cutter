using UnityEngine;

public class MouseSlice : MonoBehaviour {

    bool dragging;
    Vector3 start;
    Vector3 end;

    LineRenderer line;

    public GameObject plane;
    public GameObject SlicedPrefab;
    public Transform ObjectContainer;

    // How far away from the slice do we separate resulting objects
    public float separation;

    // Do we draw a plane object associated with the slice
    private Plane slicePlane = new Plane();
    public bool drawPlane;

    Ray mouseRay;
    readonly float distanceFromNearPlane = 2;

    private MeshCutter meshCutter;
    private TempMesh biggerMesh, smallerMesh;

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
        // Initialize a somewhat big array so that it doesn't resize hopefully?
        meshCutter = new MeshCutter(256);   
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;
	}

    // Update is called once per frame
    void Update() {
        if (!dragging && Input.GetMouseButtonDown(0))
        {
            line.positionCount = 2;
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

            // Get depth in the direction of the camera to the mouse point
            var depthAxis = mouseRay.direction.normalized;

            var planeTangent = (end - start).normalized;

            // if we didn't drag, we set tangent to be on x
            if (planeTangent == Vector3.zero)
                planeTangent = Vector3.right;

            var normalVec = Vector3.Cross(depthAxis, planeTangent);

            if (drawPlane) DrawPlane(normalVec);

            SliceObjects(start, normalVec);

            // When it's done, remove the line
            line.positionCount = 0;
        }
    }

    void SliceObjects(Vector3 point, Vector3 normal)
    {
        var toSlice = GameObject.FindGameObjectsWithTag("Sliceable");
        GameObject obj;
        for (int i = 0; i < toSlice.Length; ++i)
        {
            obj = toSlice[i];
            // We multiply by the inverse transpose of the worldToLocal Matrix, a.k.a the transpose of the localToWorld Matrix
            // Since this is how normal are transformed
            var transformedNormal = ((Vector3)(obj.transform.localToWorldMatrix.transpose * normal)).normalized;

            //Convert plane in object's local frame
            slicePlane.SetNormalAndPosition(
                transformedNormal,
                obj.transform.InverseTransformPoint(point));

            SliceObject(ref slicePlane, obj);
        }
    }

    void SliceObject(ref Plane slicePlane, GameObject obj)
    {
        var mesh = obj.GetComponent<MeshFilter>().mesh;

        if (!meshCutter.SliceMesh(mesh, ref slicePlane))
            // If we didn't slice the object then no need to separate it into 2 objects
            return;

        // TODO: Update center of mass

        // Silly condition that labels which mesh is bigger to keep the bigger mesh in the original gameobject
        bool posBigger = meshCutter.PositiveMesh.surfacearea > meshCutter.NegativeMesh.surfacearea;
        if (posBigger)
        {
            biggerMesh = meshCutter.PositiveMesh;
            smallerMesh = meshCutter.NegativeMesh;
        }
        else
        {
            biggerMesh = meshCutter.NegativeMesh;
            smallerMesh = meshCutter.PositiveMesh;
        }

        // Create new Sliced object with the other mesh
        GameObject newObject = Instantiate(obj, ObjectContainer);
        newObject.transform.SetPositionAndRotation(obj.transform.position, obj.transform.rotation);
        var newObjMesh = newObject.GetComponent<MeshFilter>().mesh;

        // Put the bigger mesh in the original object
        // Ignore colliders for now
        ReplaceMesh(mesh, biggerMesh);
        ReplaceMesh(newObjMesh, smallerMesh);

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
        mesh.SetVertices(tempMesh.vertices);
        mesh.SetTriangles(tempMesh.triangles, 0);
        mesh.SetNormals(tempMesh.normals);
        mesh.SetUVs(0, tempMesh.uvs);
        
        //mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        if (collider != null && collider.enabled)
        {
            collider.sharedMesh = mesh;
            collider.convex = true;
        }
    }

    void SeparateMeshes(Transform posTransform, Transform negTransform, Vector3 normal)
    {
        // Bring back normal in world space
        Vector3 worldNormal = ((Vector3)(posTransform.worldToLocalMatrix.transpose * normal)).normalized;

        Vector3 separationVec = worldNormal * separation;
        // Transform direction in world coordinates
        posTransform.position += separationVec;
        negTransform.position -= separationVec;
    }
}
