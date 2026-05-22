using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CustomCursorController : MonoBehaviour
{
    static CustomCursorController instance;

    [Header("Cursor")]
    public Sprite cursorSprite;
    public Vector2 cursorSize = new Vector2(32f, 32f);
    public Vector2 hotspot = Vector2.zero;
    public bool hideSystemCursor = true;
    public int sortingOrder = 32760;

    Canvas canvas;
    RectTransform cursorRect;
    Image cursorImage;

    void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCursorUI();
        ApplySettings();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplySettings();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (instance == this)
            Cursor.visible = true;
    }

    void OnValidate()
    {
        cursorSize.x = Mathf.Max(1f, cursorSize.x);
        cursorSize.y = Mathf.Max(1f, cursorSize.y);

        if (cursorImage)
            ApplySettings();
    }

    void LateUpdate()
    {
        if (!canvas || !cursorRect || !cursorImage)
            BuildCursorUI();

        ApplySettings();

        if (!cursorRect) return;

        cursorRect.position = (Vector2)Input.mousePosition - hotspot;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BuildCursorUI();
        ApplySettings();
    }

    void BuildCursorUI()
    {
        if (canvas) return;

        GameObject canvasObject = new GameObject("CustomCursorCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        canvasObject.AddComponent<CanvasScaler>();

        GameObject cursorObject = new GameObject("CustomCursorImage");
        cursorObject.transform.SetParent(canvasObject.transform, false);

        cursorRect = cursorObject.AddComponent<RectTransform>();
        cursorRect.anchorMin = cursorRect.anchorMax = new Vector2(0f, 0f);
        cursorRect.pivot = new Vector2(0f, 1f);

        cursorImage = cursorObject.AddComponent<Image>();
        cursorImage.raycastTarget = false;
    }

    void ApplySettings()
    {
        Cursor.visible = !hideSystemCursor;

        if (canvas)
        {
            canvas.sortingOrder = sortingOrder;
            canvas.gameObject.SetActive(true);
        }

        if (cursorRect)
            cursorRect.sizeDelta = cursorSize;

        if (cursorImage)
        {
            cursorImage.sprite = cursorSprite;
            cursorImage.preserveAspect = true;
            cursorImage.enabled = cursorSprite;
            cursorImage.gameObject.SetActive(true);
        }
    }
}
