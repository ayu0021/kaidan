using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgressState : MonoBehaviour
{
    public static GameProgressState Instance { get; private set; }

    readonly HashSet<string> completedEvents = new HashSet<string>();
    readonly HashSet<string> collectedPickups = new HashSet<string>();
    readonly Dictionary<string, Vector3> scenePlayerPositions = new Dictionary<string, Vector3>();

    string currentSceneName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureInstance()
    {
        GetOrCreateInstance();
    }

    public static GameProgressState GetOrCreateInstance()
    {
        if (Instance)
            return Instance;

        GameObject obj = new GameObject(nameof(GameProgressState));
        return obj.AddComponent<GameProgressState>();
    }

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentSceneName = SceneManager.GetActiveScene().name;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void LateUpdate()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(activeScene) || activeScene == "轉場布幕_") return;

        GameObject player = FindPlayer();
        if (!player) return;

        currentSceneName = activeScene;
        scenePlayerPositions[currentSceneName] = player.transform.position;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        StartCoroutine(RestorePlayerPositionNextFrame(scene.name));
    }

    IEnumerator RestorePlayerPositionNextFrame(string sceneName)
    {
        yield return null;

        if (!scenePlayerPositions.TryGetValue(sceneName, out Vector3 position))
            yield break;

        GameObject player = FindPlayer();
        if (!player)
            yield break;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb)
            rb.position = position;

        player.transform.position = position;
    }

    GameObject FindPlayer()
    {
        try
        {
            return GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            return null;
        }
    }

    public bool HasCompletedEvent(string eventId)
    {
        return !string.IsNullOrWhiteSpace(eventId) && completedEvents.Contains(eventId);
    }

    public void MarkEventCompleted(string eventId)
    {
        if (!string.IsNullOrWhiteSpace(eventId))
            completedEvents.Add(eventId);
    }

    public bool HasCollectedPickup(string pickupId)
    {
        return !string.IsNullOrWhiteSpace(pickupId) && collectedPickups.Contains(pickupId);
    }

    public void MarkPickupCollected(string pickupId)
    {
        if (!string.IsNullOrWhiteSpace(pickupId))
            collectedPickups.Add(pickupId);
    }

    public void ResetAllProgress()
    {
        completedEvents.Clear();
        collectedPickups.Clear();
        scenePlayerPositions.Clear();
        currentSceneName = SceneManager.GetActiveScene().name;
    }
}
