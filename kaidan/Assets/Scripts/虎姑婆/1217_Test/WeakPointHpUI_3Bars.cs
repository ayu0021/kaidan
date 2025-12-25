using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WeakPointHpUI_3Bars : MonoBehaviour
{
    [Header("3 Bars (從左到右)")]
    public GameObject bar1;
    public GameObject bar2;
    public GameObject bar3;

    [Header("Hit Flash (受擊變色)")]
    public bool enableHitFlash = true;
    public Color hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float flashTime = 0.08f;

    [Tooltip("若不指定，會在 Awake 自動記錄每格原本顏色")]
    public Color baseColorOverride = new Color(0, 0, 0, 0); // alpha=0 表示不用 override

    [Tooltip("低於這個值就算 0（避免浮點誤差）")]
    public float epsilon = 0.001f;

    // cached
    Image[] _imgs = new Image[3];
    SpriteRenderer[] _srs = new SpriteRenderer[3];
    Color[] _baseColors = new Color[3];
    Coroutine _flashCo;
    int _filled; // 目前亮幾格

    void Awake()
    {
        CacheRefsAndBaseColors();
    }

    void CacheRefsAndBaseColors()
    {
        var bars = new GameObject[] { bar1, bar2, bar3 };

        for (int i = 0; i < 3; i++)
        {
            _imgs[i] = null;
            _srs[i] = null;
            _baseColors[i] = Color.white;

            if (!bars[i]) continue;

            _imgs[i] = bars[i].GetComponent<Image>();
            _srs[i] = bars[i].GetComponent<SpriteRenderer>();

            // 記錄原色（或用 override）
            if (baseColorOverride.a > 0.0001f)
            {
                _baseColors[i] = baseColorOverride;
            }
            else
            {
                if (_imgs[i]) _baseColors[i] = _imgs[i].color;
                else if (_srs[i]) _baseColors[i] = _srs[i].color;
                else _baseColors[i] = Color.white;
            }
        }
    }

    /// <summary>弱點血量更新（由 WeakPoint 呼叫）</summary>
    public void Refresh(float hp, float maxHp)
    {
        if (maxHp <= 0f) maxHp = 1f;

        float ratio = Mathf.Clamp01(hp / maxHp);
        int filled = Mathf.CeilToInt(ratio * 3f);
        if (hp <= epsilon) filled = 0;

        _filled = filled;

        if (bar1) bar1.SetActive(filled >= 1);
        if (bar2) bar2.SetActive(filled >= 2);
        if (bar3) bar3.SetActive(filled >= 3);

        // 更新後先回到正常顏色（避免掉格後還停留在 hit 色）
        ApplyBaseColors();
    }

    /// <summary>受擊時呼叫：讓目前亮著的格閃一下顏色</summary>
    public void OnHit()
    {
        if (!enableHitFlash) return;

        if (_flashCo != null) StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        ApplyHitColorToFilled();

        float t = 0f;
        while (t < flashTime)
        {
            t += Time.unscaledDeltaTime; // 不怕你 Time.timeScale = 0
            yield return null;
        }

        ApplyBaseColors();
        _flashCo = null;
    }

    void ApplyHitColorToFilled()
    {
        for (int i = 0; i < 3; i++)
        {
            // 只有亮著的格才變色
            bool active = (i < _filled);
            if (!active) continue;

            if (_imgs[i]) _imgs[i].color = hitColor;
            if (_srs[i]) _srs[i].color = hitColor;
        }
    }

    void ApplyBaseColors()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_imgs[i]) _imgs[i].color = _baseColors[i];
            if (_srs[i]) _srs[i].color = _baseColors[i];
        }
    }
}
