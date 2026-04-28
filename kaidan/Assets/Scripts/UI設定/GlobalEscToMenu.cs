using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalEscToMenu : MonoBehaviour
{
    private static GlobalEscToMenu instance;

    [Header("主畫面場景名稱")]
    public string mainMenuSceneName = "start";

    [Header("Debug")]
    public bool showDebugLog = true;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (showDebugLog)
            Debug.Log("[GlobalEscToMenu] 已啟動，M 鍵會回到：" + mainMenuSceneName);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (showDebugLog)
                Debug.Log("[GlobalEscToMenu] M 鍵被按到了，準備回主畫面：" + mainMenuSceneName);

            ReturnToMainMenu();
        }
    }

    void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("[GlobalEscToMenu] 主畫面場景名稱是空的！");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}