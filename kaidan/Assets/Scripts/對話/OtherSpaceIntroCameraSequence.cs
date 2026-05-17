using System.Collections;
using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class OtherSpaceIntroCameraSequence : MonoBehaviour
{
    [System.Serializable]
    public struct CameraShot
    {
        public Vector3 position;
        public Vector3 eulerAngles;
        public float fieldOfView;
        public float duration;
    }

    [Header("References")]
    public DialogueTriggerAsset openingDialogue;
    public CinemachineVirtualCamera gameplayCamera;
    public PlayerController playerController;

    [Header("Intro Camera")]
    public int introPriority = 30;
    public float blendBackDelay = 2f;
    public CameraShot[] shots;

    bool started;
    Rigidbody playerRigidbody;
    Animator playerAnimator;

    void Start()
    {
        if (started) return;
        started = true;

        ResolveReferences();
        StartCoroutine(RunSequence());
    }

    void ResolveReferences()
    {
        if (!openingDialogue)
            openingDialogue = GetComponent<DialogueTriggerAsset>();

        if (!gameplayCamera)
            gameplayCamera = FindObjectOfType<CinemachineVirtualCamera>(true);

        if (!playerController)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
                playerController = player.GetComponent<PlayerController>();
        }

        if (!playerController)
            playerController = FindObjectOfType<PlayerController>(true);

        if (playerController)
        {
            playerRigidbody = playerController.GetComponent<Rigidbody>();
            playerAnimator = playerController.GetComponentInChildren<Animator>();
        }
    }

    IEnumerator RunSequence()
    {
        SetPlayerLocked(true);

        CinemachineVirtualCamera introCamera = CreateIntroCamera();

        if (introCamera && shots != null && shots.Length > 0)
        {
            ApplyShot(introCamera, shots[0]);

            for (int i = 1; i < shots.Length; i++)
                yield return MoveToShot(introCamera, shots[i - 1], shots[i]);
        }

        if (gameplayCamera && introCamera)
        {
            introCamera.Priority = gameplayCamera.Priority - 1;
            if (blendBackDelay > 0f)
                yield return new WaitForSeconds(blendBackDelay);
        }

        if (introCamera)
            Destroy(introCamera.gameObject);

        SetPlayerLocked(false);

        if (openingDialogue)
            openingDialogue.TriggerDialogue();
    }

    void SetPlayerLocked(bool locked)
    {
        if (!playerController)
            return;

        playerController.enabled = !locked;

        if (playerRigidbody)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (locked)
            ResetPlayerAnimationToIdle();
    }

    void ResetPlayerAnimationToIdle()
    {
        if (!playerAnimator || !playerController)
            return;

        playerAnimator.SetFloat(playerController.paramSpeed, 0f);
        playerAnimator.SetBool(playerController.paramIsWalking, false);
        playerAnimator.SetBool(playerController.paramIsWalkingUp, false);
        playerAnimator.Update(0f);
    }

    CinemachineVirtualCamera CreateIntroCamera()
    {
        if (!gameplayCamera)
            return null;

        GameObject go = new GameObject("Intro Virtual Camera");
        CinemachineVirtualCamera introCamera = go.AddComponent<CinemachineVirtualCamera>();
        introCamera.Priority = introPriority;
        introCamera.m_Lens = gameplayCamera.m_Lens;
        introCamera.Follow = null;
        introCamera.LookAt = null;
        return introCamera;
    }

    void ApplyShot(CinemachineVirtualCamera camera, CameraShot shot)
    {
        camera.transform.position = shot.position;
        camera.transform.rotation = Quaternion.Euler(shot.eulerAngles);
        camera.m_Lens.FieldOfView = shot.fieldOfView > 0f ? shot.fieldOfView : camera.m_Lens.FieldOfView;
    }

    IEnumerator MoveToShot(CinemachineVirtualCamera camera, CameraShot from, CameraShot to)
    {
        float duration = Mathf.Max(0.01f, to.duration);
        float t = 0f;
        Quaternion fromRotation = Quaternion.Euler(from.eulerAngles);
        Quaternion toRotation = Quaternion.Euler(to.eulerAngles);
        float fromFov = from.fieldOfView > 0f ? from.fieldOfView : camera.m_Lens.FieldOfView;
        float toFov = to.fieldOfView > 0f ? to.fieldOfView : fromFov;

        while (t < duration)
        {
            t += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            camera.transform.position = Vector3.LerpUnclamped(from.position, to.position, eased);
            camera.transform.rotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, eased);
            camera.m_Lens.FieldOfView = Mathf.LerpUnclamped(fromFov, toFov, eased);
            yield return null;
        }

        ApplyShot(camera, to);
    }
}
