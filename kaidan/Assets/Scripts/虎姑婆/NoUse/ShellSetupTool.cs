using UnityEngine;

[ExecuteAlways]
public class ShellSetupTool : MonoBehaviour
{
    public bool applyNow = false;

    public bool addRigidbody = true;
    public bool rbKinematic = true;
    public bool rbUseGravity = true;

    public bool addMeshCollider = true;
    public bool meshColliderConvex = true;

    public string shellLayerName = "Shell";

    void OnValidate()
    {
        if (!applyNow) return;
        applyNow = false;

        int shellLayer = LayerMask.NameToLayer(shellLayerName);
        if (shellLayer < 0)
        {
            Debug.LogError($"[ShellSetupTool] 找不到 Layer: {shellLayerName}");
            return;
        }

        int count = 0;
        foreach (Transform t in transform)
        {
            var go = t.gameObject;
            go.layer = shellLayer;

            if (addMeshCollider)
            {
                var mc = go.GetComponent<MeshCollider>();
                if (mc == null) mc = go.AddComponent<MeshCollider>();
                mc.convex = meshColliderConvex;
            }

            if (addRigidbody)
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null) rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = rbKinematic;
                rb.useGravity = rbUseGravity;
            }

            count++;
        }

        Debug.Log($"[ShellSetupTool] Applied to {count} pieces under {name}");
    }
}
