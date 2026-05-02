using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InnerShieldBarrier : MonoBehaviour
{
    [Header("Vanish")]
    public float fadeTime = 0.5f;
    public ParticleSystem vanishVfxPrefab;
    public Transform vfxSpawnPoint;

    [Header("Render / Physics")]
    public Renderer[] renderers;
    public Collider[] colliders;
    public bool deactivateGameObjectWhenHidden = false;

    MaterialPropertyBlock _mpb;
    bool _inited;
    bool _vanished;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

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

        _inited = true;
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