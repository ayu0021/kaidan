using System.Collections;
using UnityEngine;

public class WeakPoint : MonoBehaviour
{
    [Header("HP")]
    public int hp = 5;

    [Header("Damage Gate")]
    public bool canTakeDamage = false;

    [Header("Fade Out")]
    public float fadeDuration = 1.2f;

    Collider _col;
    Renderer[] _renderers;
    MaterialPropertyBlock _mpb;
    bool _dead;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        _col = GetComponent<Collider>();
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
    }

    public void SetDamageEnabled(bool enabled) => canTakeDamage = enabled;

    public void TakeDamage(int dmg = 1)
    {
        if (_dead) return;
        if (!canTakeDamage) return;

        hp -= dmg;
        if (hp <= 0) Die();
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;

        if (_col) _col.enabled = false;
        StartCoroutine(FadeOutThenDisable());
    }

    IEnumerator FadeOutThenDisable()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / fadeDuration));
            SetAlphaAll(a);
            yield return null;
        }

        SetAlphaAll(0f);
        gameObject.SetActive(false); // 你也可以改 Destroy(gameObject);
    }

    void SetAlphaAll(float a)
    {
        if (_renderers == null) return;

        foreach (var r in _renderers)
        {
            if (!r) continue;
            var mat = r.sharedMaterial;
            if (!mat) continue;

            r.GetPropertyBlock(_mpb);

            // 只改 alpha，不動 RGB
            if (mat.HasProperty(BaseColorId))
            {
                var c = mat.GetColor(BaseColorId);
                c.a = a;
                _mpb.SetColor(BaseColorId, c);
            }
            else if (mat.HasProperty(ColorId))
            {
                var c = mat.GetColor(ColorId);
                c.a = a;
                _mpb.SetColor(ColorId, c);
            }

            r.SetPropertyBlock(_mpb);
        }
    }
}

