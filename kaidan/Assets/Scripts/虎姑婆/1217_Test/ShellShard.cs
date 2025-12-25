using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ShellShard : MonoBehaviour
{
    [Header("State")]
    public bool detached;

    [Header("VFX (instant)")]
    public ParticleSystem breakVfxPrefab;   // 碎裂粒子
    public Transform vfxSpawnPoint;         // 不填就用自身位置
    public float destroyAfter = 0.8f;       // 粒子播完刪掉（可調）

    [Header("Hover Highlight")]
    public bool hoverScaleHint = true;
    public float hoverScaleMul = 1.03f;
    public bool hoverEmissionHint = true;
    public float emissionBoost = 2.5f;      // 發光強度（看材質支援）

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
    }

    public void SetHover(bool on)
    {
        if (detached) return;

        // 1) 縮放提示（保底一定有效）
        if (hoverScaleHint)
            transform.localScale = on ? _baseScale * hoverScaleMul : _baseScale;

        // 2) 發光提示（材質支援 emission 才會亮，但不會噴錯）
        if (!hoverEmissionHint || _renderers == null) return;

        foreach (var r in _renderers)
        {
            if (!r) continue;
            var mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            // 取一個基準色（拿得到就用，拿不到用白色）
            Color baseC = Color.white;
            if (mat.HasProperty(BaseColorId)) baseC = mat.GetColor(BaseColorId);
            else if (mat.HasProperty(ColorId)) baseC = mat.GetColor(ColorId);

            if (mat.HasProperty(EmissionColorId))
            {
                _mpb.SetColor(EmissionColorId, on ? baseC * emissionBoost : Color.black);
            }

            r.SetPropertyBlock(_mpb);
        }
    }

    /// <summary>
    /// 被剪掉：立刻碎裂粒子 + 自己立刻消失（不掉落）
    /// </summary>
    public void Detach(Vector3 fromPoint)
    {
        if (detached) return;
        detached = true;

        SetHover(false);

        if (_controller) _controller.OnShardDetached(this);

        // 播粒子
        if (breakVfxPrefab)
        {
            Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
            Instantiate(breakVfxPrefab, p, Quaternion.identity);
        }

        // 立刻隱藏自己（你要的「馬上變碎塊消失」）
        HideNow();

        Destroy(gameObject, Mathf.Max(0.05f, destroyAfter));
    }

    void HideNow()
    {
        if (_col) _col.enabled = false;
        if (_renderers != null)
        {
            foreach (var r in _renderers)
                if (r) r.enabled = false;
        }
    }
}


