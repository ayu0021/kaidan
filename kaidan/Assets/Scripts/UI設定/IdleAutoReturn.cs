using UnityEngine;
using UnityEngine.SceneManagement;

public class IdleAutoReturn : MonoBehaviour
{
    [Header("Idle 設定")]
    public float idleTimeLimit = 120f; // 2分鐘

    [Header("場景")]
    public string mainMenuSceneName = "start";

    private float idleTimer = 0f;

    void Update()
    {
        if (IsUserActive())
        {
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleTimeLimit)
            {
                ReturnToMainMenu();
            }
        }
    }

    bool IsUserActive()
    {
        // 鍵盤輸入
        if (Input.anyKeyDown)
            return true;

        // 滑鼠移動或點擊
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            return true;

        if (Input.mousePosition != lastMousePosition)
        {
            lastMousePosition = Input.mousePosition;
            return true;
        }

        return false;
    }

    private Vector3 lastMousePosition;

    void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // 防止暫停狀態卡住
        SceneManager.LoadScene(mainMenuSceneName);
    }
}