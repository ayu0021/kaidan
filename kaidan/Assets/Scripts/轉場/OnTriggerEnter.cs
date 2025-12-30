using UnityEngine;

public class LevelPortal : MonoBehaviour
{
    [Header("場景設定")]
    [Tooltip("請輸入要跳轉的場景正確名稱")]
    public string targetSceneName;

    private bool isTransitioning = false; // 防止重複觸發

    // --- 3D 物理觸發 ---
    private void OnTriggerEnter(Collider other)
    {
        // 確保碰撞體標籤為 Player
        if (other.CompareTag("Player") && !isTransitioning)
        {
            ExecuteSceneChange();
        }
    }

    // --- 2D 物理觸發 (如果你的遊戲是 2D 的話) ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTransitioning)
        {
            ExecuteSceneChange();
        }
    }

    private void ExecuteSceneChange()
    {
        // 直接呼叫我們剛才建立的 SceneFader 單例
        if (SceneFader.Instance != null)
        {
            isTransitioning = true;
            Debug.Log("開始轉場至: " + targetSceneName);

            // 執行漸暗與換場邏輯
            SceneFader.Instance.TransitionToScene(targetSceneName);
        }
        else
        {
            // 如果報這個錯，代表你場景中沒有放 SceneTransitionManager 物件
            Debug.LogError("錯誤：場景中找不到 SceneFader 實例！請確保場景中有 SceneTransitionManager 物件且掛載了 SceneFader 腳本。");
        }
    }
}