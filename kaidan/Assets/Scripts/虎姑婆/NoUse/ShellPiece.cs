using System.Collections;
using UnityEngine;

public class ShellPiece : MonoBehaviour
{
    [Header("State")]
    public bool alive = true;
    public bool detached = false;

    [Header("Refs (auto)")]
    public Collider col;
    public Rigidbody rb;

    [Header("Visual")]
    public Color highlightColor = Color.white;
    public float highlightTime = 0.15f;

    Renderer[] _renderers;
    MaterialPropertyBlock _mpb;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    Color[] _origColors;
    Coroutine _hlCo;

    void Awake()
    {
        col = col ? col : GetComponent<Collider>();
        rb  = rb  ? rb  : GetComponent<Rigidbody>();

        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();

        _origColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (!r) continue;

            Color c = Color.white;
            var mat = r.sharedMaterial;
            if (mat)
            {
                if (mat.HasProperty(BaseColorId)) c = mat.GetColor(BaseColorId);
                else if (mat.HasProperty(ColorId)) c = mat.GetColor(ColorId);
            }
            _origColors[i] = c;
        }
    }

    // ===== ShellCluster 可能會呼叫的 API =====

    public void SetVisible(bool v)
    {
        if (_renderers == null) return;
        foreach (var r in _renderers)
            if (r) r.enabled = v;
    }

    public void SetCollidable(bool v)
    {
        if (col) col.enabled = v;
    }

    // ✅ 相容 ShellCluster：可用具名參數 enableGravity
    public void Detach(Vector3 dir, float impulse, bool enableGravity = true)
    {
        detached = true;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = enableGravity;
            rb.AddForce(dir.normalized * impulse, ForceMode.Impulse);
        }

        if (col) col.enabled = true;
    }

    // ✅ 相容 ShellCluster：它可能叫 Flash()
    public void Flash()
    {
        PulseHighlight();
    }

    // 如果你想讓 ShellCluster 傳不同顏色/時間，也吃得下
    public void Flash(Color color, float time = 0.15f)
    {
        highlightColor = color;
        highlightTime = time;
        PulseHighlight();
    }

    // 你自己或 PlayerCutAttack 會用
    public void PulseHighlight()
    {
        if (_hlCo != null) StopCoroutine(_hlCo);
        _hlCo = StartCoroutine(HighlightRoutine());
    }

    IEnumerator HighlightRoutine()
    {
        ApplyColor(highlightColor);
        yield return new WaitForSeconds(highlightTime);
        RestoreColor();
        _hlCo = null;
    }

    void ApplyColor(Color c)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(_mpb);
        }
    }

    void RestoreColor()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            Color c = _origColors[i];
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(_mpb);
        }
    }
}

