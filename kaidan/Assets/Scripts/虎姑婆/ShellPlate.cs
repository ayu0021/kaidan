using UnityEngine;
using System.Collections;

public class ShellPlate : MonoBehaviour
{
    [Header("State")]
    public bool detached = false;

    [Header("Layers")]
    public string detachedLayerName = "Debris";

    [Header("Vanish Animation")]
    public float vanishTime = 0.25f;
    public float shrinkTo = 0.05f;

    [Header("Hover Highlight")]
    public string colorProperty = "_Color";
    public float hoverBoost = 1.6f;

    Renderer[] renderers;
    MaterialPropertyBlock mpb;
    Color[] baseColors;
    bool hoverOn = false;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        baseColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].sharedMaterial;
            if (mat != null && mat.HasProperty(colorProperty))
                baseColors[i] = mat.GetColor(colorProperty);
            else
                baseColors[i] = Color.white;
        }
    }

    public void SetHover(bool on)
    {
        if (detached) return;
        if (hoverOn == on) return;
        hoverOn = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(mpb);

            Color c = baseColors[i];
            if (on) c *= hoverBoost; // 變亮
            mpb.SetColor(colorProperty, c);

            renderers[i].SetPropertyBlock(mpb);
        }
    }

    public void Detach()
    {
        if (detached) return;
        detached = true;

        SetHover(false); // 取消高亮

        int newLayer = LayerMask.NameToLayer(detachedLayerName);
        if (newLayer == -1) newLayer = 0;
        gameObject.layer = newLayer;

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        StartCoroutine(VanishAnim());
    }

    IEnumerator VanishAnim()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * Mathf.Clamp(shrinkTo, 0.001f, 1f);

        float t = 0f;
        while (t < vanishTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(vanishTime, 0.0001f));

            transform.localScale = Vector3.Lerp(startScale, endScale, k);
            yield return null;
        }

        Destroy(gameObject);
    }
}

