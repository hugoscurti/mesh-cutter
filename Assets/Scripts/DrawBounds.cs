using UnityEngine;

public class DrawBounds : MonoBehaviour {

    private MeshFilter filter;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (enabled)
        {
            if (!filter) filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null) return;

            var modelMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = modelMatrix;
            Gizmos.DrawWireCube(filter.sharedMesh.bounds.center, filter.sharedMesh.bounds.size);
        }
    }
}
