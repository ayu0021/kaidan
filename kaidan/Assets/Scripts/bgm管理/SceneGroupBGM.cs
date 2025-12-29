using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneGroupBGM : MonoBehaviour
{
    private static SceneGroupBGM instance;

    [Header("設定音樂")]
    public AudioSource audioSource;

    [Header("設定要播放此音樂的場景名稱")]
    public List<string> targetScenes = new List<string>();

    void Awake()
    {
        // 確保場景中只有一個 BGM 管理器
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 切換場景不銷毀
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        // 註冊場景載入事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // 取消註冊場景載入事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 檢查新載入的場景是否在清單中
        if (targetScenes.Contains(scene.name))
        {
            // 如果音樂沒在播放，就播放它
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            // 如果離開了這三個場景，停止音樂（或者你可以選擇 Destroy(gameObject)）
            audioSource.Stop();
        }
    }
}