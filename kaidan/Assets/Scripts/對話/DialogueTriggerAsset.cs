using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Load Scene After Dialogue")]
    public bool loadSceneAfterDialogue = false;

    [Tooltip("對話全部結束後要切換到的場景名稱。場景必須加入 Build Settings。")]
    public string sceneName = "";

    [Tooltip("對話結束後，等待幾秒再切換場景。")]
    public float sceneLoadDelaySeconds = 0f;

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
        {
            dialogueManager.Play(dialogueAsset);

            if (loadSceneAfterDialogue)
            {
                yield return null;

                while (dialogueManager.IsPlaying)
                {
                    yield return null;
                }

                if (sceneLoadDelaySeconds > 0f)
                    yield return new WaitForSeconds(sceneLoadDelaySeconds);

                if (!string.IsNullOrEmpty(sceneName))
                    SceneManager.LoadScene(sceneName);
            }
        }
    }
}
