using System.Collections;
using UnityEngine;

public class BattleDirector : MonoBehaviour
{
    public enum BattleEndType { None, Win, Lose }
    public enum AttackType { Wait, CandleDark, Needle, Yarn }

    [System.Serializable]
    public class BattleStep
    {
        public AttackType attackType = AttackType.Wait;
        public float preDelay = 0f;
        public float duration = 3f;
    }

    [Header("Refs")]
    public BattlePlayerController player;
    public BossShellController bossShell;
    public WeakPoint weakPoint;

    public CandleAttackController candleAttack;
    public NeedleAttackController needleAttack;
    public YarnAttackController yarnAttack;

    [Header("Player Scripts To Stop")]
    public MonoBehaviour[] playerScriptsToDisableOnEnd;

    [Header("Dialogue Endings")]
    public DialogueManager dialogueManager;
    public DialogueAsset winDialogueAsset;
    public DialogueAsset loseDialogueAsset;
    public DialogueUISkin skinOverride;

    [Header("Ending Timing")]
    public float endingDialogueDelay = 1f;

    [Header("UI Optional")]
    public GameObject winPanel;
    public GameObject gameOverPanel;

    [Header("Battle Settings")]
    public bool autoStart = true;
    public bool switchToPhase2WhenShellCleared = true;

    [Header("Candle Trigger")]
    public bool triggerCandleWhenShellsLow = true;
    public int candleTriggerRemainingShells = 3;
    public float candleTriggerDuration = 3f;
    public bool candleTriggerOnlyOnce = true;

    [Header("Phase 1 Sequence")]
    public BattleStep[] phase1Sequence;

    [Header("Phase 2 Sequence")]
    public BattleStep[] phase2Sequence;

    [Header("Cleanup Generated Attacks")]
    public string[] cleanupNameContains =
    {
        "NeedleBulletRoot",
        "NeedleBullet",
        "YarnDropRoot",
        "YarnDrop",
        "Particle System"
    };

    bool battleEnded;
    bool usingPhase2;
    bool candleTriggeredByShellCount;
    Coroutine battleCoroutine;
    Coroutine endingCoroutine;

    public bool BattleEnded => battleEnded;
    public BattleEndType CurrentEndType { get; private set; } = BattleEndType.None;

    void Start()
    {
        Time.timeScale = 1f;

        if (!dialogueManager)
            dialogueManager = FindObjectOfType<DialogueManager>();

        if (player)
            player.OnPlayerDied += HandlePlayerDied;

        if (winPanel) winPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        EnableAttackScripts(true);
        StopAllAttackControllers();

        if (autoStart)
            BeginBattle();
    }

    void OnDestroy()
    {
        if (player)
            player.OnPlayerDied -= HandlePlayerDied;
    }

    public void BeginBattle()
    {
        battleEnded = false;
        usingPhase2 = false;
        candleTriggeredByShellCount = false;
        CurrentEndType = BattleEndType.None;
        Time.timeScale = 1f;

        if (battleCoroutine != null)
            StopCoroutine(battleCoroutine);

        if (endingCoroutine != null)
            StopCoroutine(endingCoroutine);

        if (winPanel) winPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        EnableAttackScripts(true);
        EnablePlayerScripts(true);
        StopAllAttackControllers();

        if (needleAttack) needleAttack.ApplyAggressiveMode(false);
        if (yarnAttack) yarnAttack.ApplyAggressiveMode(false);

        battleCoroutine = StartCoroutine(BattleLoop());

        Debug.Log("[BattleDirector] 戰鬥開始。", this);
    }

    IEnumerator BattleLoop()
    {
        while (!battleEnded)
        {
            if (ShouldTriggerCandleAtShellCount())
            {
                yield return TriggerCandleByShellCount();
                continue;
            }

            BattleStep[] currentSequence = GetCurrentSequence();

            if (currentSequence == null || currentSequence.Length == 0)
            {
                Debug.LogWarning("[BattleDirector] 沒有設定任何攻擊 Sequence。", this);
                yield return null;
                continue;
            }

            for (int i = 0; i < currentSequence.Length; i++)
            {
                if (battleEnded) yield break;

                if (ShouldEnterPhase2())
                    EnterPhase2();

                currentSequence = GetCurrentSequence();

                if (currentSequence == null || currentSequence.Length == 0)
                    currentSequence = phase1Sequence;

                BattleStep step = currentSequence[i % currentSequence.Length];

                yield return RunStep(step);
            }

            yield return null;
        }
    }

    BattleStep[] GetCurrentSequence()
    {
        if (usingPhase2)
        {
            if (phase2Sequence != null && phase2Sequence.Length > 0)
                return phase2Sequence;

            return phase1Sequence;
        }

        return phase1Sequence;
    }

    IEnumerator RunStep(BattleStep step)
    {
        StopAllAttackControllers();

        if (step.preDelay > 0f)
            yield return WaitSafe(step.preDelay);

        if (battleEnded) yield break;

        Debug.Log($"[BattleDirector] 執行攻擊：{step.attackType}，Duration = {step.duration}", this);

        switch (step.attackType)
        {
            case AttackType.Wait:
                yield return WaitSafe(step.duration);
                break;

            case AttackType.CandleDark:
                if (candleAttack)
                    yield return candleAttack.PlayDarkPhase(step.duration);
                else
                    yield return WaitSafe(step.duration);
                break;

            case AttackType.Needle:
                if (needleAttack)
                {
                    needleAttack.enabled = true;
                    needleAttack.BeginAttack();
                }

                yield return WaitSafe(step.duration);

                if (needleAttack)
                    needleAttack.StopAttack();
                break;

            case AttackType.Yarn:
                if (yarnAttack)
                {
                    yarnAttack.enabled = true;
                    yarnAttack.BeginAttack();
                }

                yield return WaitSafe(step.duration);

                if (yarnAttack)
                    yarnAttack.StopAttack();
                break;
        }

        if (candleAttack)
            candleAttack.ForceLight();
    }

    IEnumerator WaitSafe(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (battleEnded) yield break;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    bool ShouldEnterPhase2()
    {
        if (!switchToPhase2WhenShellCleared) return false;
        if (usingPhase2) return false;
        if (!bossShell) return false;

        return bossShell.ShellCleared;
    }

    bool ShouldTriggerCandleAtShellCount()
    {
        if (!triggerCandleWhenShellsLow) return false;
        if (!candleAttack) return false;
        if (!bossShell) return false;
        if (bossShell.ShellCleared) return false;
        if (candleTriggerOnlyOnce && candleTriggeredByShellCount) return false;

        return bossShell.RemainingShellCount <= candleTriggerRemainingShells;
    }

    IEnumerator TriggerCandleByShellCount()
    {
        candleTriggeredByShellCount = true;

        StopAllAttackControllers();

        Debug.Log($"[BattleDirector] 剩下 {bossShell.RemainingShellCount} 片殼，觸發 CandleDark。", this);

        yield return candleAttack.PlayDarkPhase(candleTriggerDuration);

        if (candleAttack)
            candleAttack.ForceLight();
    }

    void EnterPhase2()
    {
        usingPhase2 = true;

        if (needleAttack)
            needleAttack.ApplyAggressiveMode(true);

        if (yarnAttack)
            yarnAttack.ApplyAggressiveMode(true);

        StopAllAttackControllers();

        Debug.Log("[BattleDirector] 進入第二階段。", this);
    }

    void HandlePlayerDied()
    {
        LoseBattle();
    }

    public void WinBattle()
    {
        EndBattle(BattleEndType.Win);
    }

    public void LoseBattle()
    {
        EndBattle(BattleEndType.Lose);
    }

    public void EndBattle(BattleEndType endType)
    {
        if (battleEnded) return;

        battleEnded = true;
        CurrentEndType = endType;

        if (battleCoroutine != null)
        {
            StopCoroutine(battleCoroutine);
            battleCoroutine = null;
        }

        StopAllAttackControllers();
        EnableAttackScripts(false);
        EnablePlayerScripts(false);
        CleanupGeneratedAttacks();

        if (endType == BattleEndType.Win)
        {
            if (winPanel) winPanel.SetActive(true);
            Debug.Log("[BattleDirector] 勝利結局。", this);
        }
        else
        {
            if (gameOverPanel) gameOverPanel.SetActive(true);
            Debug.Log("[BattleDirector] 失敗結局。", this);
        }

        Time.timeScale = 1f;
        endingCoroutine = StartCoroutine(PlayEndingDialogue(endType));
    }

    IEnumerator PlayEndingDialogue(BattleEndType endType)
    {
        if (endingDialogueDelay > 0f)
            yield return new WaitForSeconds(endingDialogueDelay);

        if (!dialogueManager)
            dialogueManager = FindObjectOfType<DialogueManager>();

        if (!dialogueManager)
        {
            Debug.LogWarning("[BattleDirector] 找不到 DialogueManager。", this);
            yield break;
        }

        DialogueAsset asset = endType == BattleEndType.Win ? winDialogueAsset : loseDialogueAsset;

        if (!asset)
        {
            Debug.LogWarning($"[BattleDirector] 沒有設定 {endType} DialogueAsset。", this);
            yield break;
        }

        dialogueManager.Play(asset, skinOverride);
    }

    void StopAllAttackControllers()
    {
        if (candleAttack)
            candleAttack.ForceLight();

        if (needleAttack)
            needleAttack.StopAttack();

        if (yarnAttack)
            yarnAttack.StopAttack();
    }

    void EnableAttackScripts(bool enabled)
    {
        if (candleAttack) candleAttack.enabled = enabled;
        if (needleAttack) needleAttack.enabled = enabled;
        if (yarnAttack) yarnAttack.enabled = enabled;
    }

    void EnablePlayerScripts(bool enabled)
    {
        if (playerScriptsToDisableOnEnd == null) return;

        foreach (MonoBehaviour script in playerScriptsToDisableOnEnd)
        {
            if (script)
                script.enabled = enabled;
        }
    }

    public void CleanupGeneratedAttacks()
    {
        int count = 0;

        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>(true);
        foreach (ParticleSystem ps in particles)
        {
            if (ps)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            if (!obj) continue;

            string objName = obj.name;
            bool shouldDestroy = false;

            foreach (string key in cleanupNameContains)
            {
                if (string.IsNullOrEmpty(key)) continue;

                if (objName.Contains(key) && objName.Contains("(Clone)"))
                {
                    shouldDestroy = true;
                    break;
                }
            }

            if (!shouldDestroy) continue;

            Destroy(obj);
            count++;
        }

        Debug.Log($"[BattleDirector] 清除場上攻擊物件數量：{count}", this);
    }

    [ContextMenu("TEST/Win Battle")]
    void TestWinBattle()
    {
        WinBattle();
    }

    [ContextMenu("TEST/Lose Battle")]
    void TestLoseBattle()
    {
        LoseBattle();
    }

    [ContextMenu("TEST/Cleanup Generated Attacks")]
    void TestCleanup()
    {
        CleanupGeneratedAttacks();
    }
}
