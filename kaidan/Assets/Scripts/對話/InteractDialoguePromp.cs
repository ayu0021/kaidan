using System.Collections.Generic;
using UnityEngine;

public class InteractDialoguePrompt : MonoBehaviour
{
    [Header("Trigger")]
    public string playerTag = "Player";
    public bool triggerOnce = false;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public DialogueAsset dialogueAsset;
    public DialogueUISkin skinOverride;

    [Header("World Prompt")]
    public Vector3 promptOffset = new Vector3(0.25f, 1.5f, 0f);
    public bool billboardToCamera = true;

    [Header("Prompt Text")]
    public string promptText = "按下 F 互動";

    [Header("Debug")]
    public bool debugLog = false;

    private static readonly List<InteractDialoguePrompt> s_candidates = new List<InteractDialoguePrompt>(64);
    private static Transform s_player;

    private bool _playerInside;
    private bool _used;

    private void Awake()
    {
        // 3D Trigger 需要 Collider.isTrigger = true
        if (TryGetComponent<Collider>(out var col) && !col.isTrigger)
            col.isTrigger = true;
    }

    private void OnDisable()
    {
        s_candidates.Remove(this);
        TryHideIfOwner();
    }

    private void OnDestroy()
    {
        s_candidates.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_used && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;

        _playerInside = true;
        s_player = other.transform;

        if (!s_candidates.Contains(this))
            s_candidates.Add(this);

        if (debugLog) Debug.Log($"[InteractDialoguePrompt] Enter {name}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInside = false;
        s_candidates.Remove(this);

        if (debugLog) Debug.Log($"[InteractDialoguePrompt] Exit {name}");

        TryHideIfOwner();
    }

    private void Update()
    {
        if (_used && triggerOnce) return;
        if (!_playerInside || s_player == null) return;

        var best = GetBestCandidate();
        if (best != this) return;

        // 取得 WorldPrompt（Instance 壞掉也會自動找）
        var wp = GetWorldPrompt();
        if (wp == null)
        {
            if (debugLog) Debug.LogWarning("[InteractDialoguePrompt] 找不到 InteractionPromptWorld。請在場景放一個 WorldPrompt 並掛 InteractionPromptWorld.cs");
            return;
        }

        // 顯示提示（靠近就顯示）
        wp.faceCamera = billboardToCamera;
        wp.Show(transform, promptText, promptOffset, true, transform);

        // 只有「最近者」吃按鍵
        if (Input.GetKeyDown(interactKey))
        {
            if (debugLog) Debug.Log($"[InteractDialoguePrompt] Press F on {name}");

            if (dialogueManager == null)
                dialogueManager = FindFirstObjectByType<DialogueManager>();

            if (dialogueManager != null && dialogueAsset != null)
                dialogueManager.Play(dialogueAsset, skinOverride);
            else
                Debug.LogWarning($"[InteractDialoguePrompt] Missing DialogueManager or DialogueAsset on {name}");

            if (triggerOnce)
            {
                _used = true;
                s_candidates.Remove(this);
            }

            // 按下互動先把提示藏起來（避免遮住對話）
            TryHideIfOwner();
        }
    }

    private static InteractDialoguePrompt GetBestCandidate()
    {
        if (s_player == null) return null;

        // 清掉壞的/離場的
        for (int i = s_candidates.Count - 1; i >= 0; i--)
        {
            var c = s_candidates[i];
            if (c == null || !c.isActiveAndEnabled || (c._used && c.triggerOnce) || !c._playerInside)
                s_candidates.RemoveAt(i);
        }

        InteractDialoguePrompt best = null;
        float bestDistSq = float.PositiveInfinity;

        for (int i = 0; i < s_candidates.Count; i++)
        {
            var c = s_candidates[i];
            Vector3 p = c.transform.position + c.promptOffset;
            float d = (s_player.position - p).sqrMagnitude;
            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = c;
            }
        }

        return best;
    }

    private static InteractionPromptWorld GetWorldPrompt()
    {
        // 先用 Instance
        var wp = InteractionPromptWorld.Instance;

        // Instance 不見了就找場景（包含 inactive）
#if UNITY_2023_1_OR_NEWER
        if (wp == null) wp = Object.FindFirstObjectByType<InteractionPromptWorld>(FindObjectsInactive.Include);
#else
        if (wp == null) wp = Object.FindObjectOfType<InteractionPromptWorld>(true);
#endif

        // 強制補回 Instance，避免下一次又 NULL
        InteractionPromptWorld.Instance = wp;
        return wp;
    }

    private void TryHideIfOwner()
    {
        var wp = GetWorldPrompt();
        if (wp != null) wp.Hide(transform);
    }
}