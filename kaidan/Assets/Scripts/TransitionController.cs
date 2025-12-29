using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionController : MonoBehaviour
{
    public float fallbackDelay = 1.0f;

    IEnumerator Start()
    {
        if (string.IsNullOrWhiteSpace(TransitionData.NextScene))
        {
            Debug.LogError("[TransitionController] NextScene is empty. You probably entered Transition scene directly.");
            yield break;
        }

        float wait = TransitionData.Delay > 0 ? TransitionData.Delay : fallbackDelay;
        yield return new WaitForSeconds(wait);

        string target = TransitionData.NextScene;

        // 清掉避免下次誤用
        TransitionData.NextScene = null;
        TransitionData.Delay = 0f;

        SceneManager.LoadScene(target);
    }
}
