using System.Collections;
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
    public bool useGeneratedDissolveParticles = true;
    public int dissolveParticleCount = 26;
    public float dissolveParticleLifetime = 0.55f;
    public float dissolveParticleSpeed = 1.4f;
    public float dissolveParticleSize = 0.12f;
    public bool fadeWhenDetached = true;
    public float detachFadeTime = 0.25f;
    public float detachScaleMul = 0.2f;
    public float detachSpinDegrees = 35f;

    [Header("Hover Highlight")]
    public bool hoverScaleHint = true;
    public float hoverScaleMul = 1.015f;
    public bool hoverEmissionHint = true;
    public float emissionBoost = 2.5f;
    public Color hoverTint = new Color(0.55f, 1f, 1f, 1f);
    public bool hoverPulse = true;
    public float hoverPulseSpeed = 8f;
    public float hoverPulseStrength = 0.035f;

    BossShellController _controller;
    Renderer[] _renderers;
    Color[] _baseColors;
    Collider _col;
    MaterialPropertyBlock _mpb;
    Vector3 _baseScale;
    bool _hovering;
    bool _available = true;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        EnsureInit();
    }

    void EnsureInit()
    {
        if (_mpb != null) return;

        _controller = GetComponentInParent<BossShellController>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _col = GetComponent<Collider>();
        _mpb = new MaterialPropertyBlock();
        _baseScale = transform.localScale;
        MigrateOldDefaultTuning();
        CacheBaseColors();

        if (!_controller)
            Debug.LogWarning($"[ShellShard] {name} 找不到 BossShellController。", this);
    }

    void Update()
    {
        if (detached || !_hovering || !hoverPulse) return;

        float pulse = 1f + Mathf.Sin(Time.time * hoverPulseSpeed) * hoverPulseStrength;

        if (hoverScaleHint)
            transform.localScale = _baseScale * hoverScaleMul * pulse;

        ApplyHoverVisual(pulse);
    }

    public void BindController(BossShellController controller)
    {
        _controller = controller;
    }

    public void SetHover(bool on)
    {
        EnsureInit();

        if (detached || !_available) return;

        _hovering = on;

        if (hoverScaleHint)
            transform.localScale = on ? _baseScale * hoverScaleMul : _baseScale;

        if (on)
            ApplyHoverVisual(1f);
        else
            RestoreVisual();
    }

    public void SetAvailable(bool available)
    {
        EnsureInit();

        if (detached) return;

        _available = available;

        if (available && !gameObject.activeSelf)
            gameObject.SetActive(true);

        _hovering = false;
        transform.localScale = _baseScale;

        if (_col)
            _col.enabled = available;

        if (_renderers != null)
        {
            foreach (Renderer r in _renderers)
            {
                if (r)
                    r.enabled = available;
            }
        }

        if (available)
            RestoreVisual();
    }

    void ApplyHoverVisual(float pulse)
    {
        EnsureInit();

        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (!r) continue;

            Color baseC = GetBaseColor(i, r);
            Color tint = Color.Lerp(baseC, hoverTint, 0.55f);
            tint *= Mathf.Max(1f, pulse);
            tint.a = baseC.a;

            if (r is SpriteRenderer sr)
            {
                sr.color = tint;
                continue;
            }

            Material mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            if (mat.HasProperty(BaseColorId))
                _mpb.SetColor(BaseColorId, tint);
            else if (mat.HasProperty(ColorId))
                _mpb.SetColor(ColorId, tint);

            if (hoverEmissionHint && mat.HasProperty(EmissionColorId))
                _mpb.SetColor(EmissionColorId, baseC * emissionBoost * Mathf.Max(1f, pulse));

            r.SetPropertyBlock(_mpb);
        }
    }

    public void Detach(Vector3 fromPoint)
    {
        EnsureInit();

        if (detached) return;

        detached = true;

        SetHover(false);
        _hovering = false;

        if (_controller)
            _controller.OnShardDetached(this);
        else
            Debug.LogWarning($"[ShellShard] {name} 被剪下，但沒有 BossShellController。", this);

        if (breakVfxPrefab)
        {
            Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
            Instantiate(breakVfxPrefab, p, Quaternion.identity);
        }

        if (useGeneratedDissolveParticles)
        {
            SpawnGeneratedDissolveParticles();
        }

        if (_col)
            _col.enabled = false;

        if (fadeWhenDetached)
            StartCoroutine(DetachVanish());
        else
        {
            HideNow();
            Destroy(gameObject, Mathf.Max(0.05f, destroyAfter));
        }
    }

    IEnumerator DetachVanish()
    {
        float fadeTime = Mathf.Max(0.01f, detachFadeTime);
        Vector3 startScale = transform.localScale;
        Vector3 endScale = _baseScale * Mathf.Max(0f, detachScaleMul);
        Quaternion startRot = transform.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, detachSpinDegrees);

        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / fadeTime);
            float eased = 1f - Mathf.Pow(1f - n, 2f);

            transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            transform.localRotation = Quaternion.Slerp(startRot, endRot, eased);
            SetAlpha(1f - eased);

            yield return null;
        }

        HideNow();
        Destroy(gameObject, Mathf.Max(0.05f, destroyAfter));
    }

    void SpawnGeneratedDissolveParticles()
    {
        Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
        GameObject particleObject = new GameObject($"{name}_DissolveParticles");
        particleObject.transform.position = p;

        ParticleSystem ps = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.08f;
        main.loop = false;
        main.startLifetime = dissolveParticleLifetime;
        main.startSpeed = dissolveParticleSpeed;
        main.startSize = dissolveParticleSize;
        main.startColor = hoverTint;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Max(1, dissolveParticleCount))
        });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(hoverTint, 0f),
                new GradientColorKey(Color.white, 0.45f),
                new GradientColorKey(hoverTint, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.65f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer psRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader)
            psRenderer.material = new Material(shader);

        ps.Play();
        Destroy(particleObject, dissolveParticleLifetime + 0.25f);
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

    void MigrateOldDefaultTuning()
    {
        if (Mathf.Approximately(hoverScaleMul, 1.03f))
            hoverScaleMul = 1.015f;

        if (Mathf.Approximately(hoverPulseStrength, 0.12f))
            hoverPulseStrength = 0.035f;

        if (Mathf.Approximately(detachFadeTime, 0.45f))
            detachFadeTime = 0.25f;
    }

    void CacheBaseColors()
    {
        if (_renderers == null)
        {
            _baseColors = new Color[0];
            return;
        }

        _baseColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            _baseColors[i] = GetRendererColor(r);
        }
    }

    Color GetBaseColor(int index, Renderer r)
    {
        if (_baseColors != null && index >= 0 && index < _baseColors.Length)
            return _baseColors[index];

        return GetRendererColor(r);
    }

    Color GetRendererColor(Renderer r)
    {
        if (!r) return Color.white;

        if (r is SpriteRenderer sr)
            return sr.color;

        Material mat = r.sharedMaterial;
        if (!mat) return Color.white;

        if (mat.HasProperty(BaseColorId))
            return mat.GetColor(BaseColorId);

        if (mat.HasProperty(ColorId))
            return mat.GetColor(ColorId);

        return Color.white;
    }

    void RestoreVisual()
    {
        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (!r) continue;

            Color baseC = GetBaseColor(i, r);

            if (r is SpriteRenderer sr)
            {
                sr.color = baseC;
                continue;
            }

            Material mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            if (mat.HasProperty(BaseColorId))
                _mpb.SetColor(BaseColorId, baseC);
            else if (mat.HasProperty(ColorId))
                _mpb.SetColor(ColorId, baseC);

            if (mat.HasProperty(EmissionColorId))
                _mpb.SetColor(EmissionColorId, Color.black);

            r.SetPropertyBlock(_mpb);
        }
    }

    void SetAlpha(float alpha)
    {
        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (!r) continue;

            Color c = GetBaseColor(i, r);
            c.a *= alpha;

            if (r is SpriteRenderer sr)
            {
                sr.color = c;
                continue;
            }

            Material mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            if (mat.HasProperty(BaseColorId))
                _mpb.SetColor(BaseColorId, c);
            else if (mat.HasProperty(ColorId))
                _mpb.SetColor(ColorId, c);

            r.SetPropertyBlock(_mpb);
        }
    }
}
