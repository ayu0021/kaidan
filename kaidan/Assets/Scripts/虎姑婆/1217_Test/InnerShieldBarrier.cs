using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InnerShieldBarrier : MonoBehaviour
{
    [Header("Look")]
    public bool applyRuntimeShieldMaterial = true;
    public Color shieldColor = new Color(0f, 0f, 0f, 0.72f);
    public Color emissionColor = new Color(0f, 0f, 0f, 1f);
    public float emissionStrength = 0f;
    public float pulseSpeed = 2.2f;
    public float pulseAlpha = 0.1f;

    [Header("Vanish")]
    public float fadeTime = 0.5f;
    public ParticleSystem vanishVfxPrefab;
    public Transform vfxSpawnPoint;
    public bool useGeneratedShatterVfx = true;
    public int shatterParticleCount = 64;
    public float shatterLifetime = 0.75f;
    public float shatterSpeed = 3.2f;
    public float shatterSize = 0.16f;

    [Header("Render / Physics")]
    public Renderer[] renderers;
    public Collider[] colliders;
    public bool deactivateGameObjectWhenHidden = false;

    MaterialPropertyBlock _mpb;
    bool _inited;
    bool _vanished;
    Material[] _runtimeMaterials;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        EnsureInit();
    }

    void OnEnable()
    {
        EnsureInit();
    }

    void EnsureInit()
    {
        if (_inited) return;

        _mpb = new MaterialPropertyBlock();

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>(true);

        if (applyRuntimeShieldMaterial)
            ApplyShieldMaterial();

        _inited = true;
    }

    void Update()
    {
        if (!applyRuntimeShieldMaterial || _vanished || renderers == null) return;

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAlpha;
        float alpha = Mathf.Clamp01(shieldColor.a + pulse);
        Color c = shieldColor;
        c.a = alpha;

        foreach (Renderer r in renderers)
        {
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            SetBlockColor(r.sharedMaterial, _mpb, c);
            if (r.sharedMaterial && r.sharedMaterial.HasProperty(EmissionColorId))
                _mpb.SetColor(EmissionColorId, emissionColor * emissionStrength * (1f + Mathf.Max(0f, pulse)));
            r.SetPropertyBlock(_mpb);
        }
    }

    public void SetLocked(bool locked)
    {
        EnsureInit();

        if (_vanished && locked)
            _vanished = false;

        if (colliders != null)
        {
            foreach (Collider c in colliders)
            {
                if (c)
                    c.enabled = locked;
            }
        }

        SetVisible(locked);
        SetAlphaAll(locked ? 1f : 0f);
    }

    public IEnumerator Vanish()
    {
        EnsureInit();

        if (_vanished)
            yield break;

        _vanished = true;

        if (vanishVfxPrefab)
        {
            Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
            Instantiate(vanishVfxPrefab, p, Quaternion.identity);
        }

        if (useGeneratedShatterVfx)
            SpawnShatterParticles();

        if (colliders != null)
        {
            foreach (Collider c in colliders)
            {
                if (c)
                    c.enabled = false;
            }
        }

        if (fadeTime <= 0f)
        {
            SetAlphaAll(0f);
            SetVisible(false);
            yield break;
        }

        SetVisible(true);

        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / fadeTime));
            SetAlphaAll(a);
            yield return null;
        }

        SetAlphaAll(0f);
        SetVisible(false);
    }

    public void SetVisible(bool on)
    {
        EnsureInit();

        if (on && !gameObject.activeSelf)
            gameObject.SetActive(true);

        if (renderers != null)
        {
            foreach (Renderer r in renderers)
            {
                if (r)
                    r.enabled = on;
            }
        }

        if (!on && deactivateGameObjectWhenHidden)
            gameObject.SetActive(false);
    }

    void SetAlphaAll(float a)
    {
        EnsureInit();

        if (renderers == null) return;

        foreach (Renderer r in renderers)
        {
            if (!r) continue;

            Material mat = r.sharedMaterial;
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

    void ApplyShieldMaterial()
    {
        if (renderers == null) return;

        _runtimeMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (!r) continue;

            Material mat = CreateTransparentMaterial();
            if (!mat) continue;

            _runtimeMaterials[i] = mat;
            r.sharedMaterial = mat;
        }
    }

    Material CreateTransparentMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader)
            shader = Shader.Find("Standard");
        if (!shader)
            shader = Shader.Find("Sprites/Default");
        if (!shader)
            return null;

        Material mat = new Material(shader);
        mat.name = "Runtime_InnerShield_Glow";

        if (mat.HasProperty(BaseColorId))
            mat.SetColor(BaseColorId, shieldColor);
        if (mat.HasProperty(ColorId))
            mat.SetColor(ColorId, shieldColor);
        if (mat.HasProperty(EmissionColorId))
            mat.SetColor(EmissionColorId, emissionColor * emissionStrength);

        mat.renderQueue = 3000;
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_EMISSION");

        return mat;
    }

    void SetBlockColor(Material mat, MaterialPropertyBlock block, Color color)
    {
        if (!mat) return;

        if (mat.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, color);
        if (mat.HasProperty(ColorId))
            block.SetColor(ColorId, color);
    }

    void SpawnShatterParticles()
    {
        Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
        GameObject obj = new GameObject("InnerShield_ShatterVFX");
        obj.transform.position = p;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.08f;
        main.loop = false;
        main.startLifetime = shatterLifetime;
        main.startSpeed = shatterSpeed;
        main.startSize = shatterSize;
        main.startColor = shieldColor;
        main.gravityModifier = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Max(1, shatterParticleCount))
        });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.3f;

        ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
        if (renderer)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateTransparentMaterial();
        }

        Destroy(obj, shatterLifetime + 0.5f);
    }

    [ContextMenu("TEST/Lock")]
    void TestLock()
    {
        SetLocked(true);
    }

    [ContextMenu("TEST/Unlock")]
    void TestUnlock()
    {
        SetLocked(false);
    }

    [ContextMenu("TEST/Vanish")]
    void TestVanish()
    {
        StartCoroutine(Vanish());
    }
}
