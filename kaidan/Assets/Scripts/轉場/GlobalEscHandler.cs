using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalEscHandler : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "start";
    
    private static GlobalEscHandler instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}