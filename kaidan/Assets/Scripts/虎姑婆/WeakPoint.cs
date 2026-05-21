using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WeakPoint : MonoBehaviour
{
    [Header("Legacy HP")]
    public int maxHp = 3;
    public int hp = 3;

    [Header("Percent HP")]
    public float maxPercent = 100f;
    public float currentPercent = 100f;
    public float baseDamagePercent = 5f;
    public int comboRampStart = 999;
    public float comboBonusPercent = 0f;
    public float comboResetTime = 1.2f;

    [Header("Damage Gate")]
    public bool canTakeDamage = false;

    [Header("Crystal Look")]
    public bool applyRuntimeCrystalMaterial = true;
    public Color crystalColor = new Color(0f, 0f, 0f, 1f);
    public Color crystalEmission = new Color(0f, 0f, 0f, 1f);
    public float crystalEmissionStrength = 0f;
    public float idlePulseSpeed = 2.4f;
    public float idlePulseStrength = 0.04f;

    [Header("Fade Out")]
    public float fadeDuration = 1.2f;

    [Header("Hit Effect")]
    public bool enableHitFlash = true;
    public Color hitFlashColor = new Color(1f, 0.15f, 0.25f, 1f);
    public float hitFlashTime = 0.15f;
    public float hitScalePunch = 1.16f;
    public bool useGeneratedHitParticles = true;
    public int hitParticleCount = 18;

    [Header("Death VFX")]
    public ParticleSystem deathVfxPrefab;
    public Transform vfxSpawnPoint;
    public bool vfxUseUnscaledTime = false;

    [Header("HP UI")]
    public GameObject hpUI;
    public GameObject[] hpIcons;
    public bool createTopCenterPercentUI = true;
    public Vector2 hpBarSize = new Vector2(520f, 34f);
    public Vector2 hpBarAnchoredPosition = new Vector2(0f, -46f);
    public Color hpBarFillColor = new Color(0.1f, 0.95f, 1f, 0.92f);
    public Color hpBarBackColor = new Color(0f, 0f, 0f, 0.56f);
    public Color hpTextColor = Color.black;

    [Header("Events")]
    public UnityEvent onHit;
    public UnityEvent onBroken;

    Renderer[] _renderers;
    Collider[] _colliders;
    MaterialPropertyBlock _mpb;
    bool _dead;
    int _comboCount;
    float _lastHitTime = -999f;
    Vector3 _baseScale;
    GameObject _runtimeHpRoot;
    RectTransform _runtimeHpFillRect;
    Image _runtimeHpFill;
    Text _runtimeHpText;
    Coroutine _hitRoutine;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _mpb = new MaterialPropertyBlock();
        _baseScale = transform.localScale;

        if (currentPercent <= 0f)
            currentPercent = maxPercent;

        hp = Mathf.Clamp(hp <= 0 ? maxHp : hp, 0, maxHp);

        ApplyCrystalMaterial();
        EnsurePercentUI();
        SetDamageEnabled(canTakeDamage);
        RefreshHpUI();
    }

    void Update()
    {
        if (_dead || !applyRuntimeCrystalMaterial || _renderers == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * idlePulseSpeed) * idlePulseStrength;
        Color c = crystalColor * pulse;
        c.a = crystalColor.a;

        foreach (Renderer r in _renderers)
        {
            if (!r) continue;

            Material mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);
            SetBlockColor(mat, _mpb, c);
            if (mat.HasProperty(EmissionColorId))
                _mpb.SetColor(EmissionColorId, crystalEmission * crystalEmissionStrength * pulse);
            r.SetPropertyBlock(_mpb);
        }
    }

    public void SetDamageEnabled(bool enabled)
    {
        canTakeDamage = enabled;

        if (_colliders != null)
        {
            foreach (Collider c in _colliders)
            {
                if (c)
                    c.enabled = true;
            }
        }

        if (hpUI)
            hpUI.SetActive(false);

        if (_runtimeHpRoot)
            _runtimeHpRoot.SetActive(enabled && !_dead);

        Debug.Log($"[WeakPoint] Damage Enabled = {enabled}", this);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_dead) return;

        if (!canTakeDamage)
        {
            Debug.Log("[WeakPoint] 被打到，但目前還不能受傷。", this);
            return;
        }

        float now = Time.time;
        if (now - _lastHitTime > comboResetTime)
            _comboCount = 0;

        _comboCount++;
        _lastHitTime = now;

        float dmg = baseDamagePercent;
        currentPercent = Mathf.Max(0f, currentPercent - dmg);
        hp = Mathf.CeilToInt((currentPercent / Mathf.Max(1f, maxPercent)) * maxHp);

        Debug.Log($"[WeakPoint] 受到傷害 {dmg:0.#}% Combo={_comboCount}，剩餘 {currentPercent:0.#}%", this);

        RefreshHpUI();
        onHit?.Invoke();

        if (_hitRoutine != null)
            StopCoroutine(_hitRoutine);
        _hitRoutine = StartCoroutine(HitEffect(hitPoint));

        if (currentPercent <= 0f)
            BreakWeakPoint();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position, Vector3.up);
    }

    void BreakWeakPoint()
    {
        if (_dead) return;

        _dead = true;
        canTakeDamage = false;
        currentPercent = 0f;
        RefreshHpUI();

        Debug.Log("[WeakPoint] 弱點已擊破。", this);

        if (_colliders != null)
        {
            foreach (Collider c in _colliders)
            {
                if (c)
                    c.enabled = false;
            }
        }

        if (_runtimeHpRoot)
            _runtimeHpRoot.SetActive(false);

        if (deathVfxPrefab)
        {
            Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
            Instantiate(deathVfxPrefab, p, Quaternion.identity);
        }

        onBroken?.Invoke();
        StartCoroutine(FadeAndHide());
    }

    IEnumerator HitEffect(Vector3 hitPoint)
    {
        if (useGeneratedHitParticles)
            SpawnHitParticles(hitPoint);

        if (enableHitFlash)
        {
            SetColorOverride(hitFlashColor, true);
            transform.localScale = _baseScale * hitScalePunch;
        }

        yield return new WaitForSeconds(hitFlashTime);

        if (enableHitFlash)
        {
            SetColorOverride(Color.white, false);
            transform.localScale = _baseScale;
        }

        _hitRoutine = null;
    }

    IEnumerator FadeAndHide()
    {
        if (fadeDuration <= 0f)
        {
            SetVisible(false);
            yield break;
        }

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / fadeDuration));
            SetAlpha(a);
            yield return null;
        }

        SetVisible(false);
    }

    void RefreshHpUI()
    {
        if (hpIcons != null)
        {
            for (int i = 0; i < hpIcons.Length; i++)
            {
                if (hpIcons[i])
                    hpIcons[i].SetActive(false);
            }
        }

        RefreshPercentUI();
    }

    void SetVisible(bool visible)
    {
        if (_renderers == null) return;

        foreach (Renderer r in _renderers)
        {
            if (r)
                r.enabled = visible;
        }
    }

    void SetAlpha(float alpha)
    {
        if (_renderers == null) return;

        foreach (Renderer r in _renderers)
        {
            if (!r) continue;

            Material mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            if (mat.HasProperty(BaseColorId))
            {
                Color c = mat.GetColor(BaseColorId);
                c.a = alpha;
                _mpb.SetColor(BaseColorId, c);
            }
            else if (mat.HasProperty(ColorId))
            {
                Color c = mat.GetColor(ColorId);
                c.a = alpha;
                _mpb.SetColor(ColorId, c);
            }

            r.SetPropertyBlock(_mpb);
        }
    }

    void SetColorOverride(Color color, bool on)
    {
        if (_renderers == null) return;

        foreach (Renderer r in _renderers)
        {
            if (!r) continue;

            Material mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            if (on)
            {
                SetBlockColor(mat, _mpb, color);

                if (mat.HasProperty(EmissionColorId))
                    _mpb.SetColor(EmissionColorId, color * 3f);
            }
            else
            {
                _mpb.Clear();
            }

            r.SetPropertyBlock(_mpb);
        }
    }

    void ApplyCrystalMaterial()
    {
        if (!applyRuntimeCrystalMaterial || _renderers == null) return;

        foreach (Renderer r in _renderers)
        {
            if (!r) continue;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader)
                shader = Shader.Find("Standard");
            if (!shader)
                continue;

            Material mat = new Material(shader);
            mat.name = "Runtime_WeakPoint_Crystal";
            mat.renderQueue = 3000;

            if (mat.HasProperty(BaseColorId))
                mat.SetColor(BaseColorId, crystalColor);
            if (mat.HasProperty(ColorId))
                mat.SetColor(ColorId, crystalColor);
            if (mat.HasProperty(EmissionColorId))
                mat.SetColor(EmissionColorId, crystalEmission * crystalEmissionStrength);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.92f);

            mat.EnableKeyword("_EMISSION");
            r.sharedMaterial = mat;
        }
    }

    void SetBlockColor(Material mat, MaterialPropertyBlock block, Color color)
    {
        if (!mat) return;

        if (mat.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, color);
        if (mat.HasProperty(ColorId))
            block.SetColor(ColorId, color);
    }

    void SpawnHitParticles(Vector3 hitPoint)
    {
        GameObject obj = new GameObject("WeakPoint_HitSpark");
        obj.transform.position = hitPoint;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.06f;
        main.loop = false;
        main.startLifetime = 0.35f;
        main.startSpeed = 2.2f;
        main.startSize = 0.09f;
        main.startColor = hitFlashColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Max(1, hitParticleCount))
        });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        Destroy(obj, 0.7f);
    }

    void EnsurePercentUI()
    {
        if (!createTopCenterPercentUI || _runtimeHpRoot) return;

        GameObject canvasObj = new GameObject("WeakPointPercentCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObj.AddComponent<GraphicRaycaster>();

        _runtimeHpRoot = new GameObject("WeakPointPercentBar");
        _runtimeHpRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRt = _runtimeHpRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 1f);
        rootRt.anchorMax = new Vector2(0.5f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = hpBarAnchoredPosition;
        rootRt.sizeDelta = hpBarSize;

        Image back = _runtimeHpRoot.AddComponent<Image>();
        back.color = hpBarBackColor;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(_runtimeHpRoot.transform, false);
        _runtimeHpFillRect = fillObj.AddComponent<RectTransform>();
        _runtimeHpFillRect.anchorMin = new Vector2(0f, 0f);
        _runtimeHpFillRect.anchorMax = new Vector2(1f, 1f);
        _runtimeHpFillRect.offsetMin = Vector2.zero;
        _runtimeHpFillRect.offsetMax = Vector2.zero;
        _runtimeHpFill = fillObj.AddComponent<Image>();
        _runtimeHpFill.color = hpBarFillColor;
        _runtimeHpFill.type = Image.Type.Simple;

        GameObject textObj = new GameObject("PercentText");
        textObj.transform.SetParent(_runtimeHpRoot.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        _runtimeHpText = textObj.AddComponent<Text>();
        _runtimeHpText.alignment = TextAnchor.MiddleCenter;
        _runtimeHpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _runtimeHpText.fontSize = 22;
        _runtimeHpText.fontStyle = FontStyle.Bold;
        _runtimeHpText.color = hpTextColor;

        RefreshPercentUI();
        _runtimeHpRoot.SetActive(canTakeDamage && !_dead);
    }

    void RefreshPercentUI()
    {
        if (!_runtimeHpRoot)
            EnsurePercentUI();

        float ratio = Mathf.Clamp01(currentPercent / Mathf.Max(1f, maxPercent));

        if (_runtimeHpFill)
            _runtimeHpFill.color = hpBarFillColor;

        if (_runtimeHpFillRect)
        {
            _runtimeHpFillRect.anchorMax = new Vector2(ratio, 1f);
            _runtimeHpFillRect.offsetMin = Vector2.zero;
            _runtimeHpFillRect.offsetMax = Vector2.zero;
        }

        if (_runtimeHpText)
        {
            _runtimeHpText.color = hpTextColor;
            _runtimeHpText.text = $"{Mathf.CeilToInt(currentPercent)}%";
        }
    }

    [ContextMenu("TEST/Damage 5%")]
    void TestDamage()
    {
        TakeDamage(baseDamagePercent, transform.position, Vector3.up);
    }

    [ContextMenu("TEST/Enable Damage")]
    void TestEnableDamage()
    {
        SetDamageEnabled(true);
    }

    [ContextMenu("TEST/Break")]
    void TestBreak()
    {
        currentPercent = baseDamagePercent;
        TakeDamage(baseDamagePercent, transform.position, Vector3.up);
    }
}
