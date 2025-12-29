using System.Collections;
using UnityEngine;

public class DialogueTriggerAsset : MonoBehaviour
{
    [Header("References")]
    public DialogueManager dialogueManager;
    public DialogueAsset dialogueAsset;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("Delay")]
    public float delaySeconds = 0f;

    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;
        StartCoroutine(TriggerRoutine());
    }

    IEnumerator TriggerRoutine()
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        if (dialogueManager != null && dialogueAsset != null)
            dialogueManager.Play(dialogueAsset);
    }
}
