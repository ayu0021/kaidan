using System.Collections;
using UnityEngine;

public class CandleAttackController : MonoBehaviour
{
    [Header("Refs")]
    public BattlePlayerController player;
    public CanvasGroup darkOverlay;
    public GameObject stopWarningText;

    [Header("Dark Settings")]
    [Range(0f, 1f)] public float darkAlpha = 0.75f;
    public float fadeTime = 0.25f;

    [Header("Warning Timing")]
    [Tooltip("文字出現後，幾秒後才開始禁止移動")]
    public float warningLeadTime = 1.0f;

    void Awake()
    {
        ForceLight();
    }

    /// <summary>
    /// restrictionDuration = 真正開始「不能動」後，持續多久
    /// </summary>
    public IEnumerator PlayDarkPhase(float restrictionDuration)
    {
        // 1. 先顯示預告，但此時還沒暗下來，也還沒限制玩家
        if (stopWarningText != null)
            stopWarningText.SetActive(true);

        // 2. 給玩家幾秒反應時間
        float warningTimer = 0f;
        while (warningTimer < warningLeadTime)
        {
            if (player != null && player.IsDead)
                yield break;

            warningTimer += Time.deltaTime;
            yield return null;
        }

        // 3. 預告結束後才開始漸暗
        yield return FadeOverlay(darkAlpha);

        if (player != null)
            player.SetDamageImmune(true);

        // 4. 變暗後才正式開始「不能動」
        if (player != null)
        {
            player.SetMovementLocked(true);
            player.BeginNoMoveCheck();
        }

        // 5. 正式限制階段
        float timer = 0f;
        while (timer < restrictionDuration)
        {
            if (player != null && player.IsDead)
                break;

            timer += Time.deltaTime;
            yield return null;
        }

        // 6. 回到正常
        if (player != null)
        {
            player.EndNoMoveCheck();
            player.SetMovementLocked(false);
        }

        if (stopWarningText != null)
            stopWarningText.SetActive(false);

        yield return FadeOverlay(0f);

        if (player != null)
            player.SetDamageImmune(false);
    }

    public void ForceLight()
    {
        if (darkOverlay != null)
            darkOverlay.alpha = 0f;

        if (stopWarningText != null)
            stopWarningText.SetActive(false);

        if (player != null)
        {
            player.EndNoMoveCheck();
            player.SetMovementLocked(false);
            player.SetDamageImmune(false);
        }
    }

    private IEnumerator FadeOverlay(float targetAlpha)
    {
        if (darkOverlay == null || fadeTime <= 0f)
        {
            if (darkOverlay != null)
                darkOverlay.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = darkOverlay.alpha;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            darkOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeTime);
            yield return null;
        }

        darkOverlay.alpha = targetAlpha;
    }
}
