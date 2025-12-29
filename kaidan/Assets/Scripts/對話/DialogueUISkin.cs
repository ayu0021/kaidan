using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue UI Skin")]
public class DialogueUISkin : ScriptableObject
{
    [Header("Prefab (must have DialogueUIRefs)")]
    public DialogueUIRefs uiPrefab;

    [Header("Defaults")]
    [Tooltip("打字機速度（字/秒），<=0 代表由 DialogueManager 端決定")]
    public float charsPerSecond = 40f;

    [Tooltip("結束時是否把 Root 關掉（SetActive(false)）。不關也行，看你要不要重複使用 UI。")]
    public bool hideRootOnClose = true;

    [Tooltip("最後一句播完後，需要再按一次才關閉 UI")]
    public bool requireExtraPressToClose = false;
}
