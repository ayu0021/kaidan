using System.Collections.Generic;
using UnityEngine;

public class SpriteSheetFrameAnimator : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] Texture2D sheetTexture;
    [SerializeField, Min(1)] int columns = 10;
    [SerializeField, Min(1)] int rows = 3;
    [SerializeField, Min(0.01f)] float framesPerSecond = 8f;
    [SerializeField, Min(1f)] float pixelsPerUnit = 100f;
    [SerializeField] bool playOnAwake = true;
    [SerializeField] bool loop = true;

    [Header("Placement")]
    [SerializeField] string rootName = "老虎戰鬥_透明修正_v2";
    [SerializeField] string parentName = "背景";
    [SerializeField] string animatedObjectName = "老虎背後的線_動畫";
    [SerializeField] Vector3 localPosition = new Vector3(0f, 0.02f, -0.02f);
    [SerializeField] Vector3 localEulerAngles = Vector3.zero;
    [SerializeField] Vector3 localScale = new Vector3(0.12f, 0.12f, 0.12f);
    [SerializeField] int sortingOrder = 1;
    [SerializeField] Color color = Color.white;

    readonly List<Sprite> frames = new();
    SpriteRenderer targetRenderer;
    float frameTimer;
    int frameIndex;
    bool isPlaying;

    void Start()
    {
        BuildOrRefresh();
        isPlaying = playOnAwake;
    }

    void OnDestroy()
    {
        ClearGeneratedFrames();
    }

    void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        framesPerSecond = Mathf.Max(0.01f, framesPerSecond);
        pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
    }

    void Update()
    {
        if (targetRenderer == null)
        {
            BuildOrRefresh();
        }

        if (!isPlaying || targetRenderer == null || frames.Count == 0)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;

            if (frameIndex >= frames.Count)
            {
                if (loop)
                {
                    frameIndex = 0;
                }
                else
                {
                    frameIndex = frames.Count - 1;
                    isPlaying = false;
                }
            }

            targetRenderer.sprite = frames[frameIndex];
        }
    }

    public void BuildOrRefresh()
    {
        if (sheetTexture == null)
        {
            Debug.LogWarning("[SpriteSheetFrameAnimator] Missing sheet texture.", this);
            return;
        }

        BuildFrames();

        Transform parent = FindTargetParent();
        if (parent == null)
        {
            Debug.LogWarning($"[SpriteSheetFrameAnimator] Cannot find parent '{parentName}'.", this);
            return;
        }

        Transform child = parent.Find(animatedObjectName);
        if (child == null)
        {
            GameObject childObject = new GameObject(animatedObjectName);
            child = childObject.transform;
            child.SetParent(parent, false);
        }

        child.localPosition = localPosition;
        child.localRotation = Quaternion.Euler(localEulerAngles);
        child.localScale = localScale;

        targetRenderer = child.GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
        {
            targetRenderer = child.gameObject.AddComponent<SpriteRenderer>();
        }

        frameIndex = Mathf.Clamp(frameIndex, 0, Mathf.Max(0, frames.Count - 1));
        targetRenderer.sprite = frames.Count > 0 ? frames[frameIndex] : null;
        targetRenderer.sortingOrder = sortingOrder;
        targetRenderer.color = color;
    }

    void BuildFrames()
    {
        ClearGeneratedFrames();

        int frameWidth = sheetTexture.width / columns;
        int frameHeight = sheetTexture.height / rows;
        int frameCount = columns * rows;

        for (int i = 0; i < frameCount; i++)
        {
            int column = i % columns;
            int rowFromTop = i / columns;
            int y = sheetTexture.height - frameHeight * (rowFromTop + 1);
            Rect rect = new Rect(column * frameWidth, y, frameWidth, frameHeight);
            Sprite sprite = Sprite.Create(sheetTexture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            frames.Add(sprite);
        }
    }

    void ClearGeneratedFrames()
    {
        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i] != null)
            {
                Destroy(frames[i]);
            }
        }

        frames.Clear();
    }

    Transform FindTargetParent()
    {
        Transform root = null;
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == rootName)
            {
                root = allTransforms[i];
                break;
            }
        }

        if (root != null)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == parentName)
                {
                    return children[i];
                }
            }
        }

        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == parentName)
            {
                return allTransforms[i];
            }
        }

        return null;
    }
}
