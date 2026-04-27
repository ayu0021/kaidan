using System.Collections;
using UnityEngine;

public class TutorialTriggerFadeUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup tutorialCanvasGroup;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("Fade Setting")]
    public float fadeInTime = 0.5f;
    public float stayTime = 3f;
    public float fadeOutTime = 0.5f;

    private bool hasTriggered = false;
    private Coroutine fadeCoroutine;

    void Start()
    {
        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = 0f;
            tutorialCanvasGroup.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (triggerOnce && hasTriggered)
            return;

        hasTriggered = true;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(ShowTutorial());
    }

    IEnumerator ShowTutorial()
    {
        tutorialCanvasGroup.gameObject.SetActive(true);

        yield return Fade(0f, 1f, fadeInTime);
        yield return new WaitForSeconds(stayTime);
        yield return Fade(1f, 0f, fadeOutTime);

        tutorialCanvasGroup.gameObject.SetActive(false);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            tutorialCanvasGroup.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }

        tutorialCanvasGroup.alpha = to;
    }
}