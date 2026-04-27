using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalIdleAutoReturn : MonoBehaviour
{
    public static GlobalIdleAutoReturn Instance;

    [Header("靜置時間，180 秒 = 3 分鐘")]
    public float idleTimeLimit = 180f;

    [Header("主畫面場景名稱")]
    public string mainMenuSceneName = "start";

    private float idleTimer = 0f;
    private Vector3 lastMousePosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        lastMousePosition = Input.mousePosition;
    }

    void Update()
    {
        if (IsUserActive())
        {
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.unscaledDeltaTime;

            if (idleTimer >= idleTimeLimit)
            {
                ReturnToMainMenu();
            }
        }
    }

    bool IsUserActive()
    {
        if (Input.anyKeyDown)
            return true;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return true;

        if (Input.mousePosition != lastMousePosition)
        {
            lastMousePosition = Input.mousePosition;
            return true;
        }

        return false;
    }

    void ReturnToMainMenu()
    {
        idleTimer = 0f;
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}