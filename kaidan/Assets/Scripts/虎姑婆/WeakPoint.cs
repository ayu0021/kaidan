using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeakPoint : MonoBehaviour
{
    [Header("HP")]
    public float maxHp = 5f;
    public float hp = 5f;

    [Header("Damage Gate")]
    [SerializeField] private bool _canTakeDamage = true;
    public bool canTakeDamage => _canTakeDamage;

    [Header("Fade Out")]
    public float fadeDuration = 1.2f;

    [Header("Hit Flash (換顏色受擊特效)")]
    public bool enableHitFlash = true;
    public Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float hitFlashTime = 0.08f;

    [Header("Death VFX (粒子消散)")]
    [Tooltip("弱點死亡時噴的粒子 Prefab（ParticleSystem）")]
    public ParticleSystem deathVfxPrefab;

    [Tooltip("粒子生成位置（不填就用自己 transform.position）")]
    public Transform vfxSpawnPoint;

    [Tooltip("粒子是否使用 Unscaled Time（避免你 WIN 時 Time.timeScale=0 粒子不播）")]
    public bool vfxUseUnscaledTime = true;

    [Header("HP UI (三格血條)")]
    [Tooltip("拖入 WeakPointHpUI_3Bars（可不填）")]
    public WeakPointHpUI_3Bars hpUI;

    [Header("Auto References (可不填)")]
    public Renderer[] renderers;
    public Collider[] colliders;

    private MaterialPropertyBlock _mpb;
    private Coroutine _flashCo;
    private bool _breaking;
    private Color[] _baseColors;
    private float _currentAlpha = 1f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/HDRP
    static readonly int ColorId     = Shader.PropertyToID("_Color");     // Built-in / Legacy

    void Awake()
    {
        if (hp <= 0f) hp = maxHp;

        _mpb = new MaterialPropertyBlock();

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>(true);

        CacheBaseColors();
        SetAlphaAll(1f);

        RefreshHpUI();
    }

    void CacheBaseColors()
    {
        _baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            var mat = r ? r.sharedMaterial : null;

            if (!mat)
            {
                _baseColors[i] = Color.white;
                continue;
            }

            if (mat.HasProperty(BaseColorId)) _baseColors[i] = mat.GetColor(BaseColorId);
            else if (mat.HasProperty(ColorId)) _baseColors[i] = mat.GetColor(ColorId);
            else _baseColors[i] = Color.white;

            _baseColors[i].a = 1f;
        }
    }

    // 外殼清空後開弱點（舊流程兼容）
    public void SetDamageEnabled(bool enabled)
    {
        _canTakeDamage = enabled;

        if (colliders != null)
            foreach (var c in colliders) if (c) c.enabled = enabled;

        if (enabled)
        {
            if (renderers != null) foreach (var r in renderers) if (r) r.enabled = true;
            SetAlphaAll(1f);
        }

        RefreshHpUI();
    }

    // 兼容舊呼叫
    public void TakeDamage(float damage) =>
        TakeDamage(damage, transform.position, -transform.forward);

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!_canTakeDamage) return;
        if (_breaking) return;
        if (hp <= 0f) return;

        hp -= damage;
        if (hp < 0f) hp = 0f;

        // UI 更新 + 受擊變色（你要的那行已經寫進來）
        RefreshHpUI();
        if (hpUI) hpUI.OnHit();

        // 本體受擊閃色
        if (enableHitFlash)
        {
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(HitFlash());
        }

        if (hp <= 0f)
        {
            StartCoroutine(BreakAndVanish());
        }
    }

    IEnumerator HitFlash()
    {
        SetColorAll(hitFlashColor, _currentAlpha);

        float t = 0f;
        while (t < hitFlashTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        RestoreBaseColorAll(_currentAlpha);
        _flashCo = null;
    }

    IEnumerator BreakAndVanish()
    {
        _breaking = true;

        // 1) 死亡粒子消散
        SpawnDeathVfx();

        // 2) 先關碰撞避免淡出期間還能被打
        if (colliders != null)
            foreach (var c in colliders) if (c) c.enabled = false;

        // 3) 淡出或直接消失
        if (fadeDuration <= 0f)
        {
            if (renderers != null) foreach (var r in renderers) if (r) r.enabled = false;
            gameObject.SetActive(false);
            yield break;
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / fadeDuration));
            SetAlphaAll(a);
            yield return null;
        }

        if (renderers != null) foreach (var r in renderers) if (r) r.enabled = false;
        gameObject.SetActive(false);
    }

    void SpawnDeathVfx()
    {
        if (!deathVfxPrefab) return;

        Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
        Quaternion q = vfxSpawnPoint ? vfxSpawnPoint.rotation : Quaternion.identity;

        var ps = Instantiate(deathVfxPrefab, p, q);

        // 避免你 WIN 時 Time.timeScale=0 粒子不播
        if (vfxUseUnscaledTime)
        {
            var main = ps.main;
            main.useUnscaledTime = true;
        }

        ps.Play(true);

        // 自動清掉粒子
        float life = 2.5f;
        try
        {
            var main = ps.main;
            float dur = main.duration;
            float lt = 0.5f;

            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                lt = main.startLifetime.constantMax;
            else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                lt = main.startLifetime.constant;

            life = dur + lt + 0.5f;
        }
        catch { /* ignore */ }

        Destroy(ps.gameObject, life);
    }

    void RefreshHpUI()
    {
        if (hpUI) hpUI.Refresh(hp, maxHp);
    }

    void SetAlphaAll(float a)
    {
        _currentAlpha = a;

        if (enableHitFlash && _flashCo != null)
            SetColorAll(hitFlashColor, a);
        else
            RestoreBaseColorAll(a);
    }

    void RestoreBaseColorAll(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r || !r.sharedMaterial) continue;

            Color c = _baseColors[i];
            c.a = alpha;
            ApplyColor(r, c);
        }
    }

    void SetColorAll(Color color, float alpha)
    {
        Color c = color; c.a = alpha;
        foreach (var r in renderers)
            if (r && r.sharedMaterial) ApplyColor(r, c);
    }

    void ApplyColor(Renderer r, Color c)
    {
        var mat = r.sharedMaterial;
        r.GetPropertyBlock(_mpb);

        if (mat.HasProperty(BaseColorId)) _mpb.SetColor(BaseColorId, c);
        else if (mat.HasProperty(ColorId)) _mpb.SetColor(ColorId, c);

        r.SetPropertyBlock(_mpb);
    }
}
