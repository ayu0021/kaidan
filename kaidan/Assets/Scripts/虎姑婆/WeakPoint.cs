using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class WeakPoint : MonoBehaviour
{
    [Header("HP")]
    public int maxHp = 3;
    public int hp = 3;

    [Header("Damage Gate")]
    public bool canTakeDamage = false;

    [Header("Fade Out")]
    public float fadeDuration = 1.2f;

    [Header("Hit Flash")]
    public bool enableHitFlash = true;
    public Color hitFlashColor = Color.red;
    public float hitFlashTime = 0.15f;

    [Header("Death VFX")]
    public ParticleSystem deathVfxPrefab;
    public Transform vfxSpawnPoint;
    public bool vfxUseUnscaledTime = false;

    [Header("HP UI")]
    public GameObject hpUI;
    public GameObject[] hpIcons;

    [Header("Events")]
    public UnityEvent onHit;
    public UnityEvent onBroken;

    Renderer[] _renderers;
    Collider[] _colliders;
    MaterialPropertyBlock _mpb;
    bool _dead;
    bool _flashing;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _mpb = new MaterialPropertyBlock();

        hp = Mathf.Clamp(hp <= 0 ? maxHp : hp, 0, maxHp);

        SetDamageEnabled(canTakeDamage);
        RefreshHpUI();
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
            hpUI.SetActive(enabled && !_dead);

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

        int dmg = Mathf.Max(1, Mathf.RoundToInt(damage));
        hp = Mathf.Max(0, hp - dmg);

        Debug.Log($"[WeakPoint] 受到傷害 {dmg}，剩餘 HP = {hp}", this);

        RefreshHpUI();
        onHit?.Invoke();

        if (enableHitFlash && !_flashing)
            StartCoroutine(HitFlash());

        if (hp <= 0)
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

        Debug.Log("[WeakPoint] 弱點已擊破。", this);

        if (_colliders != null)
        {
            foreach (Collider c in _colliders)
            {
                if (c)
                    c.enabled = false;
            }
        }

        if (hpUI)
            hpUI.SetActive(false);

        if (deathVfxPrefab)
        {
            Vector3 p = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
            Instantiate(deathVfxPrefab, p, Quaternion.identity);
        }

        onBroken?.Invoke();

        StartCoroutine(FadeAndHide());
    }

    IEnumerator HitFlash()
    {
        _flashing = true;

        SetColorOverride(hitFlashColor, true);

        yield return new WaitForSeconds(hitFlashTime);

        SetColorOverride(Color.white, false);

        _flashing = false;
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
        if (hpIcons == null) return;

        for (int i = 0; i < hpIcons.Length; i++)
        {
            if (hpIcons[i])
                hpIcons[i].SetActive(i < hp);
        }
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
                if (mat.HasProperty(BaseColorId))
                    _mpb.SetColor(BaseColorId, color);

                if (mat.HasProperty(ColorId))
                    _mpb.SetColor(ColorId, color);

                if (mat.HasProperty(EmissionColorId))
                    _mpb.SetColor(EmissionColorId, color * 2f);
            }
            else
            {
                _mpb.Clear();
            }

            r.SetPropertyBlock(_mpb);
        }
    }

    [ContextMenu("TEST/Damage 1")]
    void TestDamage()
    {
        TakeDamage(1, transform.position, Vector3.up);
    }

    [ContextMenu("TEST/Enable Damage")]
    void TestEnableDamage()
    {
        SetDamageEnabled(true);
    }

    [ContextMenu("TEST/Break")]
    void TestBreak()
    {
        hp = 1;
        TakeDamage(1, transform.position, Vector3.up);
    }
}