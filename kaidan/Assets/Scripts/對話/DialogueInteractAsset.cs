using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueInteractAsset : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public GameObject dialogueRoot;
    public DialogueAsset dialogueAsset;

    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("Prompt (Optional)")]
    public GameObject promptUI; // 例如 "按 E 互動" 的 UI（可不填）

    bool playerInside = false;
    bool triggered = false;

    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && triggered) return;

        playerInside = true;
        if (promptUI != null) promptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        if (!playerInside) return;
        if (triggerOnce && triggered) return;

        if (GetEDown())
        {
            triggered = true;
            if (promptUI != null) promptUI.SetActive(false);

            if (dialogueRoot != null) dialogueRoot.SetActive(true);
            if (dialogueManager != null) dialogueManager.Play(dialogueAsset);
        }
    }

    bool GetEDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}

