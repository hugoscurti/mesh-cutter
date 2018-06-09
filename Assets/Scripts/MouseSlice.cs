using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSlice : MonoBehaviour {

    bool dragging;
    Vector3 start;
    Vector3 end;

    LineRenderer line;

    public GameObject plane;

    Ray mouseRay;
    readonly float distanceFromNearPlane = 2;

	// Use this for initialization
	void Start () {
        dragging = false;

        line = GetComponent<LineRenderer>();
	}

    // Update is called once per frame
    void Update() {
        //Debug.Log(Camera.main.transform.forward);
        if (Input.GetMouseButtonDown(0))
        {
            start = GetMousePosOnCamera();
            //Debug.Log("Start: " + start);
            line.SetPosition(0, start);
            dragging = true;
        } 

        if (dragging)
        {
            line.SetPosition(1, GetMousePosOnCamera());
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            end = GetMousePosOnCamera();
            //Debug.Log("End: " + end);
            line.SetPosition(1, end);
            dragging = false;

            var depthAxis = Camera.main.transform.forward;
            var planeTangent = end - start;
            var normalVec = Vector3.Cross(depthAxis, planeTangent);

            //DrawPlane(normalVec);
            Plane plane = new Plane(normalVec, start);
        }
    }

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
}
