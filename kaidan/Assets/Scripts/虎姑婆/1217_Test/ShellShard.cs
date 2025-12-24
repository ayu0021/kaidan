using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ShellShard : MonoBehaviour
{
    [Header("State")]
    public bool detached;

    [Header("Detach")]
    public float detachImpulse = 2.2f;
    public float vanishDelay = 3.0f;

    [Header("Hover")]
    public float hoverEmissionBoost = 2.0f;   // 用 emission 亮起來
    public bool alsoScaleSlightly = true;
    public float hoverScaleMul = 1.03f;

    BossShellController _controller;
    Renderer[] _renderers;
    MaterialPropertyBlock _mpb;

    Vector3 _baseScale;
    int _debrisLayer;

    // 常見 shader 屬性：抓得到就改，抓不到就用 scale 提示
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        _controller = GetComponentInParent<BossShellController>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
        _baseScale = transform.localScale;

        _debrisLayer = LayerMask.NameToLayer("Debris");
    }

    public void SetHover(bool on)
    {
        if (detached) return;

        // 1) 視覺提示：優先用 emission（最穩），沒有 emission 就不改材質，至少用縮放提示
        if (_renderers != null)
        {
            foreach (var r in _renderers)
            {
                if (!r) continue;

                var mat = r.sharedMaterial;
                if (!mat) continue;

                r.GetPropertyBlock(_mpb);

                // 嘗試拿一個基準色（有就用，沒有就白色）
                Color baseC = Color.white;
                if (mat.HasProperty(BaseColorId)) baseC = mat.GetColor(BaseColorId);
                else if (mat.HasProperty(ColorId)) baseC = mat.GetColor(ColorId);

                // emission 亮起
                if (mat.HasProperty(EmissionColorId))
                {
                    Color e = on ? baseC * hoverEmissionBoost : Color.black;
                    _mpb.SetColor(EmissionColorId, e);
                }

                r.SetPropertyBlock(_mpb);
            }
        }

        // 2) 備援提示：微縮放（就算材質完全不支援也一定看得出來）
        if (alsoScaleSlightly)
            transform.localScale = on ? _baseScale * hoverScaleMul : _baseScale;
    }

    public void Detach(Vector3 fromPoint)
    {
        if (detached) return;
        detached = true;

        SetHover(false);
        if (_controller) _controller.OnShardDetached(this);

        // 變 Debris：避免撞到還黏著的 Shell（靠 Physics 矩陣）
        if (_debrisLayer >= 0) gameObject.layer = _debrisLayer;

        // 脫離父物件（避免父物件縮放/旋轉干擾）
        transform.SetParent(null, true);

        // 動態加 Rigidbody：最穩
        var rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Vector3 dir = (transform.position - fromPoint);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
        dir.Normalize();

        rb.AddForce(dir * detachImpulse, ForceMode.Impulse);

        Destroy(gameObject, vanishDelay);
    }
}
