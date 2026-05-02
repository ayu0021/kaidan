using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BattleVictoryDialogueBridge : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public DialogueAsset dialogueAsset;
    public DialogueUISkin skinOverride;

    [Header("Timing")]
    public float delaySeconds = 0.6f;
    public bool triggerOnce = true;

    [Header("Optional Disable")]
    public GameObject[] objectsToDisable;
    public MonoBehaviour[] scriptsToDisable;

    [Header("Events")]
    public UnityEvent beforeDialogue;
    public UnityEvent afterDialogueStarted;

    bool triggered;

    public void TriggerVictoryDialogue()
    {
        if (triggerOnce && triggered) return;

        triggered = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        beforeDialogue?.Invoke();

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj)
                obj.SetActive(false);
        }

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script)
                script.enabled = false;
        }

        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        if (!dialogueManager)
            dialogueManager = FindObjectOfType<DialogueManager>();

        if (!dialogueManager)
        {
            Debug.LogWarning("[BattleVictoryDialogueBridge] 找不到 DialogueManager。", this);
            yield break;
        }

        if (!dialogueAsset)
        {
            Debug.LogWarning("[BattleVictoryDialogueBridge] 沒有指定 DialogueAsset。", this);
            yield break;
        }

        dialogueManager.Play(dialogueAsset, skinOverride);

        afterDialogueStarted?.Invoke();

        Debug.Log("[BattleVictoryDialogueBridge] 勝利對話已觸發。", this);
    }
}