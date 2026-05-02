using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ShellShard : MonoBehaviour
{
    [Header("State")]
    public bool detached;

    [Header("VFX")]
    public ParticleSystem breakVfxPrefab;
    public Transform vfxSpawnPoint;
    public float destroyAfter = 0.8f;

    [Header("Hover Highlight")]
    public bool hoverScaleHint = true;
    public float hoverScaleMul = 1.03f;
    public bool hoverEmissionHint = true;
    public float emissionBoost = 2.5f;

    BossShellController _controller;
    Renderer[] _renderers;
    Collider _col;
    MaterialPropertyBlock _mpb;
    Vector3 _baseScale;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        _controller = GetComponentInParent<BossShellController>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _col = GetComponent<Collider>();
        _mpb = new MaterialPropertyBlock();
        _baseScale = transform.localScale;

        if (!_controller)
            Debug.LogWarning($"[ShellShard] {name} 找不到 BossShellController。", this);
    }

    public void BindController(BossShellController controller)
    {
        _controller = controller;
    }

    public void SetHover(bool on)
    {
        if (detached) return;

        if (hoverScaleHint)
            transform.localScale = on ? _baseScale * hoverScaleMul : _baseScale;

        if (!hoverEmissionHint || _renderers == null) return;

        foreach (Renderer r in _renderers)
        {
            if (!r) continue;

            Material mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            Color baseC = Color.white;

            if (mat.HasProperty(BaseColorId))
                baseC = mat.GetColor(BaseColorId);
            else if (mat.HasProperty(ColorId))
                baseC = mat.GetColor(ColorId);

            if (mat.HasProperty(EmissionColorId))
                _mpb.SetColor(EmissionColorId, on ? baseC * emissionBoost : Color.black);

            r.SetPropertyBlock(_mpb);
        }
    }

    public void Detach(Vector3 fromPoint)
    {
        if (detached) return;

        detached = true;

        SetHover(false);

        if (_controller)
            _controller.OnShardDetached(this);
        else
            Debug.LogWarning($"[ShellShard] {name} 被剪下，但沒有 BossShellController。", this);

        if (breakVfxPrefab)
        {
            Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
            Instantiate(breakVfxPrefab, p, Quaternion.identity);
        }

        HideNow();

        Destroy(gameObject, Mathf.Max(0.05f, destroyAfter));
    }

    void HideNow()
    {
        if (_col)
            _col.enabled = false;

        if (_renderers != null)
        {
            foreach (Renderer r in _renderers)
            {
                if (r)
                    r.enabled = false;
            }
        }
    }
}