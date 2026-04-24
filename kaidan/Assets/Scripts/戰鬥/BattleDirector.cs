using System.Collections;
using UnityEngine;

public class BattleDirector : MonoBehaviour
{
    public enum AttackType
    {
        Wait,
        CandleDark,
        Needle,
        Yarn
    }

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

    [Header("UI")]
    public GameObject winPanel;

    [Header("Battle Settings")]
    public bool autoStart = true;
    public bool pauseTimeOnBattleEnd = true;
    public bool switchToPhase2WhenShellCleared = true;

    [Header("Phase 1 Sequence")]
    public BattleStep[] phase1Sequence;

    [Header("Phase 2 Sequence")]
    public BattleStep[] phase2Sequence;

    private bool battleEnded;
    private bool usingPhase2;
    private Coroutine battleCoroutine;

    void Start()
    {
        Time.timeScale = 1f;

        if (winPanel != null)
            winPanel.SetActive(false);

        if (player != null)
            player.OnPlayerDied += HandlePlayerDied;

        StopAllAttackControllers();

        if (autoStart)
            BeginBattle();
    }

    void OnDestroy()
    {
        if (player != null)
            player.OnPlayerDied -= HandlePlayerDied;
    }

    public void BeginBattle()
    {
        if (battleCoroutine != null)
            StopCoroutine(battleCoroutine);

        battleEnded = false;
        usingPhase2 = false;

        if (winPanel != null)
            winPanel.SetActive(false);

        if (needleAttack != null)
            needleAttack.ApplyAggressiveMode(false);

        if (yarnAttack != null)
            yarnAttack.ApplyAggressiveMode(false);

        StopAllAttackControllers();
        battleCoroutine = StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        while (!battleEnded)
        {
            BattleStep[] currentSequence = usingPhase2 ? phase2Sequence : phase1Sequence;

            if (currentSequence == null || currentSequence.Length == 0)
                yield break;

            for (int i = 0; i < currentSequence.Length; i++)
            {
                if (battleEnded) yield break;

                if (ShouldEnterPhase2())
                {
                    usingPhase2 = true;

                    if (needleAttack != null)
                        needleAttack.ApplyAggressiveMode(true);

                    if (yarnAttack != null)
                        yarnAttack.ApplyAggressiveMode(true);

                    StopAllAttackControllers();
                    break;
                }

                yield return RunStep(currentSequence[i]);

                if (IsBossDead())
                {
                    WinBattle();
                    yield break;
                }

                if (player != null && player.IsDead)
                {
                    LoseBattle();
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator RunStep(BattleStep step)
    {
        StopAllAttackControllers();

        if (step.preDelay > 0f)
            yield return WaitSafe(step.preDelay);

        switch (step.attackType)
        {
            case AttackType.Wait:
                yield return WaitSafe(step.duration);
                break;

            case AttackType.CandleDark:
                if (candleAttack != null)
                    yield return candleAttack.PlayDarkPhase(step.duration);
                else
                    yield return WaitSafe(step.duration);
                break;

            case AttackType.Needle:
                if (needleAttack != null)
                    needleAttack.BeginAttack();

                yield return WaitSafe(step.duration);

                if (needleAttack != null)
                    needleAttack.StopAttack();
                break;

            case AttackType.Yarn:
                if (yarnAttack != null)
                    yarnAttack.BeginAttack();

                yield return WaitSafe(step.duration);

                if (yarnAttack != null)
                    yarnAttack.StopAttack();
                break;
        }

        if (candleAttack != null)
            candleAttack.ForceLight();
    }

    private IEnumerator WaitSafe(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (battleEnded) yield break;
            if (player != null && player.IsDead) yield break;
            if (IsBossDead()) yield break;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private bool ShouldEnterPhase2()
    {
        if (!switchToPhase2WhenShellCleared) return false;
        if (usingPhase2) return false;
        if (bossShell == null) return false;

        return bossShell.ShellCleared;
    }

    private bool IsBossDead()
    {
        if (weakPoint == null) return false;
        return !weakPoint.gameObject.activeInHierarchy;
    }

    private void HandlePlayerDied()
    {
        LoseBattle();
    }

    private void WinBattle()
    {
        if (battleEnded) return;
        battleEnded = true;

        StopAllAttackControllers();

        if (winPanel != null)
            winPanel.SetActive(true);

        if (pauseTimeOnBattleEnd)
            Time.timeScale = 0f;
    }

    private void LoseBattle()
    {
        if (battleEnded) return;
        battleEnded = true;

        StopAllAttackControllers();

        if (pauseTimeOnBattleEnd)
            Time.timeScale = 0f;
    }

    private void StopAllAttackControllers()
    {
        if (candleAttack != null)
            candleAttack.ForceLight();

        if (needleAttack != null)
            needleAttack.StopAttack();

        if (yarnAttack != null)
            yarnAttack.StopAttack();
    }
}