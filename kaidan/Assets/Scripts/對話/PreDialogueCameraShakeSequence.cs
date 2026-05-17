using System.Collections;
using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public class PreDialogueCameraShakeSequence : MonoBehaviour
{
    [Header("References")]
    public DialogueTriggerAsset dialogueTrigger;
    public CinemachineVirtualCamera gameplayCamera;
    public PlayerController playerController;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool triggerOnce = true;

    [Header("Shake")]
    public float shakeDuration = 1.3f;
    public float shakeStrength = 0.9f;
    public float shakeFrequency = 18f;

    bool triggered;
    Rigidbody playerRigidbody;
    Animator playerAnimator;

    void Awake()
    {
        ResolveReferences();
    }

    void ResolveReferences()
    {
        if (!dialogueTrigger)
            dialogueTrigger = GetComponent<DialogueTriggerAsset>();

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

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && triggered) return;

        triggered = true;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        ResolveReferences();
        SetPlayerLocked(true);

        CinemachineTransposer transposer = gameplayCamera
            ? gameplayCamera.GetCinemachineComponent<CinemachineTransposer>()
            : null;

        if (transposer)
            yield return ShakeFollowOffset(transposer);
        else if (shakeDuration > 0f)
            yield return new WaitForSeconds(shakeDuration);

        SetPlayerLocked(false);

        if (dialogueTrigger)
            dialogueTrigger.TriggerDialogue();
    }

    IEnumerator ShakeFollowOffset(CinemachineTransposer transposer)
    {
        Vector3 baseOffset = transposer.m_FollowOffset;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            float x = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0.1f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0.1f, Time.time * shakeFrequency) - 0.5f) * 2f;
            transposer.m_FollowOffset = baseOffset + new Vector3(x, y, 0f) * shakeStrength * fade;
            yield return null;
        }

        transposer.m_FollowOffset = baseOffset;
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
}
