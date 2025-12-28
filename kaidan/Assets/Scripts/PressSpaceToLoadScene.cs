using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PressSpaceToLoadScene : MonoBehaviour
{
    [Header("Next Scene")]
    public string nextSceneName = "虎姑婆woods_";

    [Header("Input")]
    public bool allowSpace = true;
    public bool allowMouseClick = false;

    [Header("Optional Fade Out")]
    public CanvasGroup fadeOutCanvasGroup;
    public float fadeOutTime = 0.25f;

    bool isLoading = false;

    void Awake()
    {
        if (fadeOutCanvasGroup != null)
        {
            fadeOutCanvasGroup.alpha = 0f;
            fadeOutCanvasGroup.interactable = false;
            fadeOutCanvasGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (isLoading) return;

        if (allowSpace && GetSpaceDown())
        {
            StartCoroutine(LoadRoutine());
            return;
        }

        if (allowMouseClick && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(LoadRoutine());
            return;
        }
    }

    System.Collections.IEnumerator LoadRoutine()
    {
        isLoading = true;

        if (fadeOutCanvasGroup != null && fadeOutTime > 0f)
        {
            fadeOutCanvasGroup.blocksRaycasts = true;

            float t = 0f;
            while (t < fadeOutTime)
            {
                t += Time.unscaledDeltaTime;
                fadeOutCanvasGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeOutTime));
                yield return null;
            }
            fadeOutCanvasGroup.alpha = 1f;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    bool GetSpaceDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
