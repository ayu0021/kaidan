using System.Collections;
using UnityEngine;

public class InteractiveLamp : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLog = false;
    public bool drawGizmos = true;

    [Header("References")]
    [Tooltip("要控制的子物件 Light（Point/Spot）")]
    public Light targetLight;

    [Header("Trigger")]
    public string playerTag = "Player";

    [Header("Base Lighting")]
    [Tooltip("靠近後最亮強度")]
    public float maxIntensity = 6f;

    [Tooltip("離開後最暗強度（通常 0）")]
    public float minIntensity = 0f;

    [Tooltip("漸亮時間")]
    public float fadeInTime = 0.6f;

    [Tooltip("漸暗時間")]
    public float fadeOutTime = 0.8f;

    [Tooltip("靠近時 Light 的 Range（可選，不想動就填 0）")]
    public float rangeWhenOn = 6f;

    [Tooltip("離開時 Light 的 Range（可選，不想動就填 0）")]
    public float rangeWhenOff = 0f;

    [Header("Creepy Flicker（停留後詭異閃爍）")]
    [Tooltip("進入範圍後多久開始進入詭異狀態")]
    public float creepyDelay = 1.2f;

    [Tooltip("詭異程度（0=溫和、1=很詭異）")]
    [Range(0f, 1f)]
    public float creepiness = 0.75f;

    [Tooltip("平均每秒發生幾次『事件』(double flash / blackout / sputter)")]
    [Range(0.2f, 6f)]
    public float eventRate = 1.6f;

    [Tooltip("低頻漂移速度（越大越不穩）")]
    public float driftSpeed = 0.35f;

    [Tooltip("高頻抖動速度（越大越抽搐）")]
    public float jitterSpeed = 18f;

    [Tooltip("黑屏（瞬間熄滅）事件的最大時長")]
    public float blackoutMax = 0.18f;

    [Tooltip("是否允許偶爾完全熄滅（更恐怖）")]
    public bool allowBlackout = true;

    // Runtime state
    private bool inRange;
    private float currentIntensity;
    private float baseIntensity;   // 目前目標基礎亮度（通常 maxIntensity）
    private float currentRange;
    private Coroutine routine;

    private Collider myTrigger;

    private void Awake()
    {
        myTrigger = GetComponent<Collider>();

        if (targetLight == null)
            targetLight = GetComponentInChildren<Light>();

        if (targetLight == null)
        {
            Debug.LogError($"[InteractiveLamp] {name} 找不到 targetLight！請在子物件放一個 Light 或手動指定。");
            enabled = false;
            return;
        }

        // 初始關燈
        SetLightInstant(minIntensity);
        SetRangeInstant(rangeWhenOff > 0 ? rangeWhenOff : targetLight.range);

        if (debugLog)
        {
            Debug.Log($"[Lamp Awake] {name} | hasTrigger={myTrigger != null} isTrigger={(myTrigger != null && myTrigger.isTrigger)} " +
                      $"| light={targetLight.name} intensity={targetLight.intensity} range={targetLight.range}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (debugLog) Debug.Log($"[Lamp ENTER] {name} <- {other.name}");

        inRange = true;
        StartBehavior();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (debugLog) Debug.Log($"[Lamp EXIT] {name} <- {other.name}");

        inRange = false;
        StartBehavior();
    }

    private void StartBehavior()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(inRange ? TurnOnThenCreepy() : TurnOffSmooth());
    }

    private IEnumerator TurnOnThenCreepy()
    {
        baseIntensity = maxIntensity;

        // 漸亮
        if (debugLog) Debug.Log($"[Lamp] TurnOn start: {name}");
        yield return FadeTo(baseIntensity, fadeInTime, rangeWhenOn);

        // 等待進入詭異狀態
        float t = 0f;
        while (t < creepyDelay)
        {
            if (!inRange) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (debugLog) Debug.Log($"[Lamp] Creepy mode start: {name} creepiness={creepiness}");

        // 詭異閃爍主迴圈
        // 概念：低頻漂移 + 高頻抖動 + 隨機事件（double flash / sputter / blackout / stutter pause）
        float nextEventTime = Time.time + GetNextEventInterval();
        float blackoutUntil = 0f;

        while (inRange)
        {
            float dt = Time.deltaTime;

            // --- 1) 基礎漂移（讓亮度慢慢不穩） ---
            float drift = Mathf.PerlinNoise(Time.time * driftSpeed, 0.17f); // 0~1
            // 讓漂移落在 0.55~1.0 左右（越詭異下限越低）
            float driftMin = Mathf.Lerp(0.75f, 0.45f, creepiness);
            float driftFactor = Mathf.Lerp(driftMin, 1.0f, drift);

            // --- 2) 高頻抖動（抽搐感） ---
            float jitter = Mathf.PerlinNoise(Time.time * jitterSpeed, 0.83f); // 0~1
            // 讓抖動幅度隨詭異程度上升
            float jitterAmp = Mathf.Lerp(0.05f, 0.35f, creepiness);
            float jitterFactor = 1f - jitterAmp + jitter * jitterAmp;

            // --- 3) 隨機事件：偶爾更怪的行為 ---
            if (Time.time >= nextEventTime)
            {
                TriggerOneCreepyEvent(ref blackoutUntil);
                nextEventTime = Time.time + GetNextEventInterval();
            }

            // --- 4) 黑屏事件優先（瞬間熄滅） ---
            if (allowBlackout && Time.time < blackoutUntil)
            {
                // 黑屏期間：強制到極低（不是 0 也行，看你要不要“死掉”）
                float low = Mathf.Lerp(0.02f, 0f, creepiness);
                SetLightInstant(baseIntensity * low);
                yield return null;
                continue;
            }

            // --- 5) 合成亮度 ---
            float target = baseIntensity * driftFactor * jitterFactor;

            // 再加一點「偶爾突然掉亮」的機率（像電壓不穩）
            float dropChance = Mathf.Lerp(0.02f, 0.12f, creepiness);
            if (Random.value < dropChance * dt)
            {
                // 瞬降
                target *= Mathf.Lerp(0.55f, 0.2f, creepiness);
            }

            // 夾住範圍
            target = Mathf.Clamp(target, minIntensity, maxIntensity);

            // 平滑接近（讓它不會太數值感）
            float smooth = Mathf.Lerp(10f, 25f, creepiness);
            currentIntensity = Mathf.Lerp(currentIntensity, target, 1f - Mathf.Exp(-smooth * dt));
            targetLight.intensity = currentIntensity;

            yield return null;
        }

        if (debugLog) Debug.Log($"[Lamp] Creepy mode end: {name}");
    }

    private void TriggerOneCreepyEvent(ref float blackoutUntil)
    {
        // 事件種類：double flash / sputter / blackout / stutter pause
        // 用 creepiness 去調權重
        float r = Random.value;

        // 權重設定（越詭異 blackout 越常見）
        float wDouble = Mathf.Lerp(0.40f, 0.28f, creepiness);
        float wSputter = Mathf.Lerp(0.45f, 0.37f, creepiness);
        float wBlackout = Mathf.Lerp(0.10f, 0.25f, creepiness);
        float wPause = 1f - (wDouble + wSputter + wBlackout);

        if (r < wDouble)
        {
            // 兩段短促閃光：像突然電壓衝高
            StartCoroutine(DoubleFlash());
            if (debugLog) Debug.Log($"[Lamp Event] DoubleFlash: {name}");
        }
        else if (r < wDouble + wSputter)
        {
            // 噴火式抖動：快速衰減跳動一小段
            StartCoroutine(SputterBurst());
            if (debugLog) Debug.Log($"[Lamp Event] Sputter: {name}");
        }
        else if (r < wDouble + wSputter + wBlackout)
        {
            if (allowBlackout)
            {
                float dur = Random.Range(0.05f, blackoutMax);
                blackoutUntil = Time.time + dur;
                if (debugLog) Debug.Log($"[Lamp Event] Blackout {dur:0.00}s: {name}");
            }
        }
        else
        {
            // 突然“停一下”的怪感：亮度短暫凍結後再繼續
            StartCoroutine(StutterPause());
            if (debugLog) Debug.Log($"[Lamp Event] Pause: {name}");
        }
    }

    private float GetNextEventInterval()
    {
        // 平均 eventRate 次/秒，但加入隨機（越詭異越不規律）
        float mean = 1f / Mathf.Max(0.01f, eventRate);
        float chaos = Mathf.Lerp(0.25f, 0.75f, creepiness);
        // 指數分布：不規則事件間隔更自然
        float u = Mathf.Clamp01(Random.value);
        float interval = -Mathf.Log(1f - u) * mean;
        // 再加一點 chaos 變動
        interval *= Random.Range(1f - chaos, 1f + chaos);
        // 最小間隔避免太吵
        return Mathf.Clamp(interval, 0.12f, 2.2f);
    }

    private IEnumerator DoubleFlash()
    {
        // 兩次很短的超亮閃
        float boost = Mathf.Lerp(1.25f, 1.8f, creepiness);
        float d1 = Random.Range(0.03f, 0.07f);
        float gap = Random.Range(0.04f, 0.10f);
        float d2 = Random.Range(0.03f, 0.08f);

        float prev = currentIntensity;

        targetLight.intensity = Mathf.Min(maxIntensity * boost, maxIntensity * 2.2f);
        yield return new WaitForSeconds(d1);

        targetLight.intensity = prev;
        yield return new WaitForSeconds(gap);

        targetLight.intensity = Mathf.Min(maxIntensity * boost, maxIntensity * 2.2f);
        yield return new WaitForSeconds(d2);

        targetLight.intensity = prev;
    }

    private IEnumerator SputterBurst()
    {
        // 0.2~0.6 秒內多次抽搐（模擬接觸不良）
        float dur = Random.Range(0.18f, 0.55f);
        float end = Time.time + dur;

        while (Time.time < end && inRange)
        {
            float drop = Mathf.Lerp(0.75f, 0.15f, creepiness) * Random.Range(0.6f, 1f);
            targetLight.intensity = Mathf.Clamp(baseIntensity * drop, minIntensity, maxIntensity);
            yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
        }
    }

    private IEnumerator StutterPause()
    {
        // 凍結亮度一小段時間（像卡住）
        float dur = Random.Range(0.08f, Mathf.Lerp(0.18f, 0.35f, creepiness));
        float hold = targetLight.intensity;
        float end = Time.time + dur;

        while (Time.time < end && inRange)
        {
            targetLight.intensity = hold;
            yield return null;
        }
    }

    private IEnumerator TurnOffSmooth()
    {
        if (debugLog) Debug.Log($"[Lamp] TurnOff start: {name}");
        yield return FadeTo(minIntensity, fadeOutTime, rangeWhenOff);
        if (debugLog) Debug.Log($"[Lamp] TurnOff done: {name}");
    }

    private IEnumerator FadeTo(float targetIntensity, float duration, float targetRange)
    {
        float startI = currentIntensity;
        float startR = targetLight.range;

        bool changeRange = targetRange > 0f;

        if (duration <= 0f)
        {
            SetLightInstant(targetIntensity);
            if (changeRange) SetRangeInstant(targetRange);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            float i = Mathf.Lerp(startI, targetIntensity, p);
            targetLight.intensity = i;
            currentIntensity = i;

            if (changeRange)
            {
                float r = Mathf.Lerp(startR, targetRange, p);
                targetLight.range = r;
            }

            yield return null;
        }

        SetLightInstant(targetIntensity);
        if (changeRange) SetRangeInstant(targetRange);
    }

    private void SetLightInstant(float intensity)
    {
        currentIntensity = intensity;
        targetLight.intensity = intensity;
    }

    private void SetRangeInstant(float r)
    {
        currentRange = r;
        targetLight.range = r;
    }

    // ====== Debug / Tools ======
    [ContextMenu("TEST/Force Turn ON (no trigger)")]
    private void TestForceOn()
    {
        if (routine != null) StopCoroutine(routine);
        inRange = true;
        routine = StartCoroutine(TurnOnThenCreepy());
        Debug.Log($"[Lamp TEST] Force ON: {name}");
    }

    [ContextMenu("TEST/Force Turn OFF (no trigger)")]
    private void TestForceOff()
    {
        if (routine != null) StopCoroutine(routine);
        inRange = false;
        routine = StartCoroutine(TurnOffSmooth());
        Debug.Log($"[Lamp TEST] Force OFF: {name}");
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        var col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = inRange ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 1f, 0f, 0.25f);

        if (col is BoxCollider box)
        {
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = old;
        }
        else if (col is SphereCollider sphere)
        {
            float s = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), sphere.radius * s);
        }
    }
}
