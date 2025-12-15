using System.Collections;
using UnityEngine;

public class ShellPiece : MonoBehaviour
{
    [HideInInspector] public Collider col;
    [HideInInspector] public Rigidbody rb;

    [Header("State")]
    public bool alive = true;
    public bool detached = false;

    [Header("Visual")]
    public Color highlightColor = new Color(1f, 0.35f, 0.1f, 1f);
    public float highlightTime = 0.15f; // 先拉長，肉眼比較看得到

    Renderer[] _renderers;
    MaterialPropertyBlock _mpb;

    static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in/Standard
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/HDRP Lit

    void Awake()
    {
        col = GetComponent<Collider>();
        rb  = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();

        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.None; // 避免你不小心 freeze 住
        }
    }

    public void SetVisible(bool v)
    {
        if (_renderers == null) return;
        foreach (var r in _renderers) if (r) r.enabled = v;
    }

    public void SetCollidable(bool v)
    {
        if (col) col.enabled = v;
    }

    public void Flash()
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(CoFlash());
    }

    IEnumerator CoFlash()
    {
        foreach (var r in _renderers)
        {
            if (!r) continue;

            r.GetPropertyBlock(_mpb);

            // 同時寫兩種常見顏色屬性，保證你看得到
            var mat = r.sharedMaterial;
            if (mat != null && mat.HasProperty(BaseColorId)) _mpb.SetColor(BaseColorId, highlightColor);
            if (mat != null && mat.HasProperty(ColorId))     _mpb.SetColor(ColorId, highlightColor);

            r.SetPropertyBlock(_mpb);
        }

        yield return new WaitForSeconds(highlightTime);

        foreach (var r in _renderers)
        {
            if (!r) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.Clear();
            r.SetPropertyBlock(_mpb);
        }
    }

    public void Detach(Vector3 dir, float impulse, bool enableGravity = true)
    {
        detached = true;

        if (!rb) return;

        rb.isKinematic = false;
        rb.useGravity = enableGravity;
        rb.constraints = RigidbodyConstraints.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.AddForce(dir.normalized * impulse, ForceMode.Impulse);
        rb.AddTorque(Random.onUnitSphere * (impulse * 0.2f), ForceMode.Impulse);
    }
}

