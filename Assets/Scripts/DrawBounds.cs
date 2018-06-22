using UnityEngine;

public class DrawBounds : MonoBehaviour {

    private new MeshRenderer renderer;
    private MeshFilter filter;

	// Use this for initialization
	void Start () {
        renderer = GetComponent<MeshRenderer>();
        filter = GetComponent<MeshFilter>();
	}

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (renderer != null && enabled)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(filter.mesh.bounds.center, filter.mesh.bounds.size);

            // This is the renderer's bounds, which is less accurate if we account rotations
            //Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
        }



    }
}
