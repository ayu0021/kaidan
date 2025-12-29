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
        // 尋找場景中的 SceneLoader 腳本
        SceneLoader loader = Object.FindFirstObjectByType<SceneLoader>();

        if (loader != null)
        {
            isTransitioning = true;
            loader.LoadNextLevel(targetSceneName);
        }
        else
        {
            Debug.LogError("錯誤：場景中找不到 SceneLoader 腳本！請確保 Canvas 上有掛載 SceneLoader。");
        }
    }
}