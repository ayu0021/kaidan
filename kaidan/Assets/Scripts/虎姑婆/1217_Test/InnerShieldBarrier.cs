using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[DefaultExecutionOrder(-1000)] // 盡量早一點初始化（但真正保險的是懶初始化）
public class InnerShieldBarrier : MonoBehaviour
{
    [Header("Vanish")]
    [Tooltip("消散淡出時間；想瞬間消失就設 0")]
    public float fadeTime = 0.5f;

    [Tooltip("護罩消失粒子 Prefab（可不填）")]
    public ParticleSystem vanishVfxPrefab;

    [Tooltip("粒子生成位置（可不填，會用自己 transform.position）")]
    public Transform vfxSpawnPoint;

    [Header("Render / Physics")]
    [Tooltip("不填會自動抓子物件所有 Renderer")]
    public Renderer[] renderers;

    [Tooltip("不填會自動抓子物件所有 Collider（含自己）")]
    public Collider[] colliders;

    [Tooltip("隱藏時是否 SetActive(false)。一般不建議，除非你確定外部不會再呼叫它")]
    public bool deactivateGameObjectWhenHidden = false;

    private MaterialPropertyBlock _mpb;
    private bool _inited;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/HDRP
    static readonly int ColorId     = Shader.PropertyToID("_Color");     // Built-in

    void Awake() => EnsureInit();
    void OnEnable() => EnsureInit();
    void OnValidate()
    {
        if (!Application.isPlaying) EnsureInit();
    }

    private void EnsureInit()
    {
        if (_inited) return;

        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>(true);

        _inited = true;
    }

    /// <summary>
    /// locked=true：Collider 開啟（擋子彈）
    /// locked=false：Collider 關閉（可打進去）
    /// </summary>
    public void SetLocked(bool locked)
    {
        EnsureInit();

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i]) colliders[i].enabled = locked;
        }

        SetVisible(true);
        SetAlphaAll(1f);
    }

    public IEnumerator Vanish()
    {
        EnsureInit();

        if (vanishVfxPrefab)
        {
            Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
            Instantiate(vanishVfxPrefab, p, Quaternion.identity);
        }

        // 先關碰撞，避免淡出期間還擋子彈
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i]) colliders[i].enabled = false;
        }

        if (fadeTime <= 0f)
        {
            SetVisible(false);
            yield break;
        }

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / fadeTime));
            SetAlphaAll(a);
            yield return null;
        }

        SetVisible(false);
    }

    public void SetVisible(bool on)
    {
        EnsureInit();

        // 如果之前你有 deactive，想顯示要先 active 回來
        if (on && !gameObject.activeSelf)
            gameObject.SetActive(true);

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (!r) continue;
                r.enabled = on;
            }
        }

        if (!on && deactivateGameObjectWhenHidden)
            gameObject.SetActive(false);
    }

    private void SetAlphaAll(float a)
    {
        EnsureInit();

        if (renderers == null || renderers.Length == 0) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            var mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            if (mat.HasProperty(BaseColorId))
            {
                Color c = mat.GetColor(BaseColorId);
                c.a = a;
                _mpb.SetColor(BaseColorId, c);
            }
            else if (mat.HasProperty(ColorId))
            {
                Color c = mat.GetColor(ColorId);
                c.a = a;
                _mpb.SetColor(ColorId, c);
            }

            r.SetPropertyBlock(_mpb);
        }
    }

    [ContextMenu("TEST/Lock")]
    private void TestLock() => SetLocked(true);

    [ContextMenu("TEST/Unlock")]
    private void TestUnlock() => SetLocked(false);

    [ContextMenu("TEST/Vanish")]
    private void TestVanish() => StartCoroutine(Vanish());
}


