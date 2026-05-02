using System.Collections;
using UnityEngine;

public class BattleIntroDialogueStarter : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public DialogueAsset introDialogueAsset;
    public DialogueUISkin skinOverride;

    [Header("Battle")]
    public BattleDirector battleDirector;

    [Header("Timing")]
    public float startDelay = 0.5f;
    public float battleStartDelayAfterDialogue = 0.3f;

    [Header("Options")]
    public bool triggerOnSceneStart = true;
    public bool triggerOnce = true;

    bool triggered;

    void Start()
    {
        if (triggerOnSceneStart)
            TriggerIntro();
    }

    public void TriggerIntro()
    {
        if (triggerOnce && triggered) return;

        triggered = true;
        StartCoroutine(RunIntro());
    }

    IEnumerator RunIntro()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (!dialogueManager)
            dialogueManager = FindObjectOfType<DialogueManager>();

        if (!battleDirector)
            battleDirector = FindObjectOfType<BattleDirector>();

        if (!dialogueManager)
        {
            Debug.LogWarning("[BattleIntroDialogueStarter] 找不到 DialogueManager。", this);
            StartBattleDirectly();
            yield break;
        }

        if (!introDialogueAsset)
        {
            Debug.LogWarning("[BattleIntroDialogueStarter] 沒有指定開場 DialogueAsset。", this);
            StartBattleDirectly();
            yield break;
        }

        dialogueManager.Play(introDialogueAsset, skinOverride);

        yield return new WaitUntil(() => !IsDialoguePlaying());

        if (battleStartDelayAfterDialogue > 0f)
            yield return new WaitForSeconds(battleStartDelayAfterDialogue);

        StartBattleDirectly();
    }

    bool IsDialoguePlaying()
    {
        if (!dialogueManager) return false;

        // 優先嘗試常見欄位 / 屬性名稱
        var type = dialogueManager.GetType();

        var prop = type.GetProperty("IsPlaying");
        if (prop != null && prop.PropertyType == typeof(bool))
            return (bool)prop.GetValue(dialogueManager);

        prop = type.GetProperty("IsOpen");
        if (prop != null && prop.PropertyType == typeof(bool))
            return (bool)prop.GetValue(dialogueManager);

        var field = type.GetField("isPlaying");
        if (field != null && field.FieldType == typeof(bool))
            return (bool)field.GetValue(dialogueManager);

        field = type.GetField("isOpen");
        if (field != null && field.FieldType == typeof(bool))
            return (bool)field.GetValue(dialogueManager);

        // 如果你的 DialogueManager 沒有公開狀態，保底等 3 秒後開戰
        return false;
    }

    void StartBattleDirectly()
    {
        if (!battleDirector)
            battleDirector = FindObjectOfType<BattleDirector>();

        if (battleDirector)
        {
            battleDirector.BeginBattle();
            Debug.Log("[BattleIntroDialogueStarter] 開場劇情結束，戰鬥開始。", this);
        }
        else
        {
            Debug.LogWarning("[BattleIntroDialogueStarter] 找不到 BattleDirector，無法開始戰鬥。", this);
        }
    }
}