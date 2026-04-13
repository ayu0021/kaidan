using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue UI Skin")]
public class DialogueUISkin : ScriptableObject
{
    [Header("Prefab (must have DialogueUIRefs)")]
    public DialogueUIRefs uiPrefab;

    [Header("Frame Type")]
    [Tooltip("勾選代表這個 Skin 使用藍框；不勾代表使用紅框。")]
    public bool useBlueFrame = false;

    [Header("Defaults")]
    [Tooltip("打字機速度（字/秒），<=0 代表由 DialogueManager 端決定")]
    public float charsPerSecond = 40f;

    [Tooltip("結束時是否把 Root 關掉（SetActive(false)）。")]
    public bool hideRootOnClose = true;

    [Tooltip("最後一句播完後，需要再按一次才關閉 UI")]
    public bool requireExtraPressToClose = false;
}