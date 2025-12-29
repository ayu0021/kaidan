using System;
using System.Reflection;
using TMPro;
using UnityEngine;

public class InteractDialoguePrompt : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("玩家物件的 Tag（通常是 Player）")]
    public string playerTag = "Player";

    [Tooltip("只觸發一次（觸發後就不能再互動）")]
    public bool triggerOnce = false;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public DialogueAsset dialogueAsset;
    [Tooltip("可不填；不填就用 DialogueManager.defaultSkin")]
    public DialogueUISkin skinOverride;

    [Header("World Prompt")]
    [Tooltip("一定要從 Project 拖 Prefab 進來（不要拖 Hierarchy 的物件）")]
    public GameObject worldPromptPrefab;

    [Tooltip("提示在世界座標的位置偏移（相對於本 Trigger）")]
    public Vector3 promptOffset = new Vector3(0.25f, 1.5f, 0f);

    [Tooltip("是否朝向相機（簡易 Billboard）")]
    public bool billboardToCamera = true;

    [Header("Prompt Text")]
    public string promptText = "按下 F 互動";

    [Header("Debug")]
    public bool debugLog = false;

    // runtime
    private bool _playerInside;
    private bool _used;
    private Transform _player;
    private GameObject _promptGO;
    private TMP_Text _promptTMP;

    // cache camera
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;

        // 保險：確保 collider 是 trigger（你也可以自己手動勾）
        if (TryGetComponent<Collider>(out var col) && !col.isTrigger)
        {
            if (debugLog) Debug.LogWarning($"[{name}] Collider is not Trigger. Auto set isTrigger=true");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_used && triggerOnce) return;
        if (!other.CompareTag(playerTag)) return;

        _playerInside = true;
        _player = other.transform;

        if (debugLog) Debug.Log($"[InteractDialoguePrompt] Player entered trigger: {name}");
        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInside = false;
        _player = null;

        if (debugLog) Debug.Log($"[InteractDialoguePrompt] Player exited trigger: {name}");
        HidePrompt();
    }

    private void Update()
    {
        if (_used && triggerOnce) return;

        // 跟隨 & billboard（即使沒按鍵也要更新位置）
        if (_promptGO != null && _promptGO.activeSelf)
        {
            _promptGO.transform.position = transform.position + promptOffset;

            if (billboardToCamera)
            {
                if (_cam == null) _cam = Camera.main;
                if (_cam != null)
                {
                    var fwd = _cam.transform.forward;
                    // 若你想要完全面向相機（包含上下），就把這行註解掉
                    fwd.y = 0f;

                    if (fwd.sqrMagnitude > 0.0001f)
                        _promptGO.transform.forward = fwd.normalized;
                }
            }
        }

        if (!_playerInside) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (debugLog) Debug.Log($"[InteractDialoguePrompt] Interact pressed on: {name}");

            // 觸發對話
            if (dialogueManager != null && dialogueAsset != null)
            {
                dialogueManager.Play(dialogueAsset, skinOverride);
            }
            else
            {
                Debug.LogWarning($"[InteractDialoguePrompt] Missing DialogueManager or DialogueAsset on: {name}");
            }

            if (triggerOnce) _used = true;

            // 一按就先把提示藏起來（避免遮住對話）
            HidePrompt();
        }
    }

    private void ShowPrompt()
    {
        if (worldPromptPrefab == null)
        {
            Debug.LogError("[InteractDialoguePrompt] worldPromptPrefab is NULL.（請從 Project 拖 WorldPrompt.prefab 進來，不要拖 Hierarchy 的物件）");
            return;
        }

        if (_promptGO == null)
        {
            _promptGO = Instantiate(worldPromptPrefab);
            _promptGO.name = $"{worldPromptPrefab.name}(Prompt)";
        }

        _promptGO.SetActive(true);

        // 設定位址
        _promptGO.transform.position = transform.position + promptOffset;

        // 找 TMP 文字（不用依賴 WorldPromptView）
        CacheTMP();
        ApplyPromptText(promptText);

        // 如果 prefab 上剛好有 WorldPromptView，就用反射塞 followTarget / offset / billboard
        TryConfigureWorldPromptView();
    }

    private void HidePrompt()
    {
        if (_promptGO != null)
            _promptGO.SetActive(false);
    }

    private void CacheTMP()
    {
        if (_promptGO == null) return;
        if (_promptTMP != null) return;

        _promptTMP = _promptGO.GetComponentInChildren<TMP_Text>(true);
        if (debugLog && _promptTMP == null)
            Debug.LogWarning("[InteractDialoguePrompt] No TMP_Text found under worldPromptPrefab. Prompt text may not show.");
    }

    private void ApplyPromptText(string text)
    {
        if (_promptTMP != null)
            _promptTMP.text = text ?? "";
    }

    /// <summary>
    /// 不引用 WorldPromptView 型別，避免 CS0246。
    /// 但如果 prefab 上真的有 WorldPromptView，就用反射把欄位塞進去。
    /// </summary>
    private void TryConfigureWorldPromptView()
    {
        if (_promptGO == null) return;

        MonoBehaviour wpv = FindMonoBehaviourByTypeName(_promptGO, "WorldPromptView");
        if (wpv == null) return;

        // 依你之前 WorldPromptView 的欄位名稱：followTarget / worldOffset / billboardToCamera / textTMP
        SetFieldIfPossible(wpv, "followTarget", transform);
        SetFieldIfPossible(wpv, "worldOffset", promptOffset);
        SetFieldIfPossible(wpv, "billboardToCamera", billboardToCamera);

        // 若 WorldPromptView 有 textTMP 欄位，也一起同步（可選）
        var textTMPObj = GetFieldValue(wpv, "textTMP");
        if (textTMPObj is TMP_Text tmp)
        {
            tmp.text = promptText ?? "";
        }
    }

    private static MonoBehaviour FindMonoBehaviourByTypeName(GameObject root, string typeName)
    {
        var mbs = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < mbs.Length; i++)
        {
            var mb = mbs[i];
            if (mb == null) continue;

            Type t = mb.GetType();
            if (t.Name == typeName) return mb;
        }
        return null;
    }

    private static void SetFieldIfPossible(object target, string fieldName, object value)
    {
        if (target == null) return;

        var t = target.GetType();
        var f = t.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f == null) return;

        // 型別可相容才賦值
        if (value == null)
        {
            if (!f.FieldType.IsValueType) f.SetValue(target, null);
            return;
        }

        if (f.FieldType.IsAssignableFrom(value.GetType()))
            f.SetValue(target, value);
    }

    private static object GetFieldValue(object target, string fieldName)
    {
        if (target == null) return null;

        var t = target.GetType();
        var f = t.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? f.GetValue(target) : null;
    }
}
