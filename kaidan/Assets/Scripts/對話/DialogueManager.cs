using System.Collections;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("Skins")]
    public DialogueUISkin defaultSkin;
    public DialogueUISkin secondarySkin;

    [Tooltip("UI 生成後要掛在哪個父物件下（通常是 Canvas）。強烈建議指定。")]
    public Transform uiParent;

    [Header("Input")]
    public bool allowSpace = true;
    public bool allowMouseClick = true;
    public KeyCode advanceKey = KeyCode.F;

    [Header("Typewriter")]
    public bool useTypewriter = true;
    public float charsPerSecond = 40f;

    [Header("No auto text")]
    public bool autoFillNameFromCharacter = false;

    [Header("Debug")]
    public bool debugLog = true;

    private DialogueUIRefs _ui;
    private DialogueTextAnimator _bodyAnimator;
    private Coroutine _routine;
    private Coroutine _cgRoutine;

    private bool _isPlaying;
    public bool IsPlaying => _isPlaying;
    private float _activeCps;

    private DialogueUISkin _currentUISkin;

    private bool _portraitBaseCaptured;
    private Vector3 _portraitBaseLocalScale;
    private Vector2 _portraitBaseAnchoredPos;
    private Vector2 _portraitBaseSizeDelta;
    private Vector2 _portraitBaseAnchorMin;
    private Vector2 _portraitBaseAnchorMax;
    private Vector2 _portraitBasePivot;

    private Sprite _currentCgSprite;
    private bool _currentCgVisible;
    private float _currentCgAlpha;

    private void Awake()
    {
        if (advanceKey == KeyCode.Space)
            advanceKey = KeyCode.F;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (advanceKey == KeyCode.Space)
            advanceKey = KeyCode.F;
    }
#endif

    public void Play(DialogueAsset asset, DialogueUISkin skinOverride = null)
    {
        if (asset == null)
        {
            Debug.LogWarning("[DialogueManager] Play called with null asset.");
            return;
        }

        Stop();

        var startSkin = skinOverride != null ? skinOverride : defaultSkin;
        if (startSkin == null) startSkin = secondarySkin;

        if (startSkin == null || startSkin.uiPrefab == null)
        {
            Debug.LogError("[DialogueManager] No DialogueUISkin / uiPrefab assigned.");
            return;
        }

        EnsureUIInstance(startSkin);

        _ui.ShowFrame(startSkin.useBlueFrame);
        BindRuntimeRefs();
        CapturePortraitBase();

        _ui.ClearAll();
        _ui.RootGO.SetActive(true);

        ApplyCpsFromSkin(startSkin);

        _routine = StartCoroutine(PlayRoutine(asset));
    }

    public void Stop()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (_cgRoutine != null)
        {
            StopCoroutine(_cgRoutine);
            _cgRoutine = null;
        }

        _isPlaying = false;

        _currentCgSprite = null;
        _currentCgVisible = false;
        _currentCgAlpha = 0f;

        if (_bodyAnimator != null)
            _bodyAnimator.ClearEffects();

        ApplyCurrentCgImmediate();

        if (_ui != null && _currentUISkin != null && _currentUISkin.hideRootOnClose)
            _ui.RootGO.SetActive(false);
    }

    private void EnsureUIInstance(DialogueUISkin skin)
    {
        if (skin == null || skin.uiPrefab == null)
        {
            Debug.LogError("[DialogueManager] EnsureUIInstance: skin/uiPrefab is null.");
            return;
        }

        if (_ui != null && _currentUISkin != skin)
        {
            if (debugLog) Debug.Log($"[DialogueManager] Rebuild UI: {_currentUISkin?.name} -> {skin.name}");
            Destroy(_ui.gameObject);
            _ui = null;
            _bodyAnimator = null;
            _portraitBaseCaptured = false;
        }

        if (_ui != null) return;

        Transform parent = uiParent != null ? uiParent : transform;

        _ui = Instantiate(skin.uiPrefab, parent);
        _ui.name = $"DialogueUI({skin.name})";
        _currentUISkin = skin;

        RectTransform rootRt = _ui.transform as RectTransform;
        if (rootRt != null)
        {
            rootRt.localScale = Vector3.one;
            rootRt.localRotation = Quaternion.identity;
        }
        else
        {
            _ui.transform.localScale = Vector3.one;
            _ui.transform.localRotation = Quaternion.identity;
        }

        BindRuntimeRefs();

        if (debugLog)
        {
            Vector3 ls = parent.lossyScale;
            Debug.Log($"[DialogueManager] Instantiate UI under parent={parent.name}, parentLossyScale={ls}");
        }

        CapturePortraitBase();
        ApplyCurrentCgImmediate();
    }

    private void BindRuntimeRefs()
    {
        _bodyAnimator = null;

        if (_ui != null && _ui.bodyText != null)
        {
            _ui.EnsureCgBehindDialogue();

            _bodyAnimator = _ui.bodyText.GetComponent<DialogueTextAnimator>();
            if (_bodyAnimator == null)
                _bodyAnimator = _ui.bodyText.gameObject.AddComponent<DialogueTextAnimator>();

            _bodyAnimator.Bind(_ui.bodyText);
        }
    }

    private void ApplyCpsFromSkin(DialogueUISkin skin)
    {
        _activeCps = (skin != null && skin.charsPerSecond > 0f) ? skin.charsPerSecond : charsPerSecond;
        if (_activeCps <= 0f) _activeCps = 40f;
    }

    private void ApplyLineCpsOverride(DialogueLineData line)
    {
        if (line != null && line.overrideCharsPerSecond && line.charsPerSecondOverride > 0f)
            _activeCps = line.charsPerSecondOverride;
    }

    private void CapturePortraitBase()
    {
        _portraitBaseCaptured = false;

        if (_ui == null || _ui.portraitImage == null)
            return;

        RectTransform rt = _ui.portraitImage.rectTransform;

        _portraitBaseAnchoredPos = rt.anchoredPosition;
        _portraitBaseSizeDelta = rt.sizeDelta;
        _portraitBaseAnchorMin = rt.anchorMin;
        _portraitBaseAnchorMax = rt.anchorMax;
        _portraitBasePivot = rt.pivot;

        float baseSign = Mathf.Sign(rt.localScale.x);
        if (baseSign == 0f) baseSign = 1f;

        _portraitBaseLocalScale = new Vector3(baseSign, 1f, 1f);
        rt.localScale = _portraitBaseLocalScale;

        _portraitBaseCaptured = true;

        if (debugLog)
        {
            Debug.Log(
                $"[DialogueManager] CapturePortraitBase skin={_currentUISkin?.name} portrait={_ui.portraitImage.name} " +
                $"baseScale={_portraitBaseLocalScale} basePos={_portraitBaseAnchoredPos} size={_portraitBaseSizeDelta}"
            );
        }
    }

    private void RestorePortraitRect(RectTransform rt)
    {
        rt.anchorMin = _portraitBaseAnchorMin;
        rt.anchorMax = _portraitBaseAnchorMax;
        rt.pivot = _portraitBasePivot;
        rt.sizeDelta = _portraitBaseSizeDelta;
        rt.anchoredPosition = _portraitBaseAnchoredPos;
    }

    private IEnumerator PlayRoutine(DialogueAsset asset)
    {
        _isPlaying = true;

        if (asset.lines == null || asset.lines.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] DialogueAsset has no lines.");
            CloseUI();
            yield break;
        }

        for (int i = 0; i < asset.lines.Count; i++)
        {
            DialogueLineData line = asset.lines[i];
            DialogueUISkin lineSkin = ResolveSkinForLine(line);

            if (lineSkin != null && lineSkin.uiPrefab != null)
            {
                EnsureUIInstance(lineSkin);

                _ui.ShowFrame(lineSkin.useBlueFrame);
                BindRuntimeRefs();
                CapturePortraitBase();

                _ui.ClearAll();
                _ui.RootGO.SetActive(true);

                ApplyCpsFromSkin(lineSkin);
                ApplyLineCpsOverride(line);
            }

            ApplyLineToUI(line);

            yield return StartCoroutine(WaitUntilAdvanceReleased());
            yield return StartCoroutine(TypeLine(line));
            yield return StartCoroutine(WaitForAdvance());
        }

        if (_currentUISkin != null && _currentUISkin.requireExtraPressToClose)
            yield return StartCoroutine(WaitForAdvance());

        CloseUI();
    }

    private DialogueUISkin ResolveSkinForLine(DialogueLineData line)
    {
        if (line != null && line.uiSkin != null) return line.uiSkin;
        if (defaultSkin != null) return defaultSkin;
        return secondarySkin;
    }

    private void CloseUI()
    {
        _isPlaying = false;

        if (_bodyAnimator != null)
            _bodyAnimator.ClearEffects();

        _currentCgSprite = null;
        _currentCgVisible = false;
        _currentCgAlpha = 0f;
        ApplyCurrentCgImmediate();

        if (_currentUISkin != null && _currentUISkin.hideRootOnClose && _ui != null)
            _ui.RootGO.SetActive(false);
    }

    private void ApplyLineToUI(DialogueLineData line)
    {
        if (_ui == null || line == null)
            return;

        HandleCgForLine(line);

        string nameTextValue = line.nameOverride;
        if (string.IsNullOrEmpty(nameTextValue) && autoFillNameFromCharacter)
            nameTextValue = "";

        if (_ui.nameText)
            _ui.nameText.text = nameTextValue ?? "";

        if (_ui.hintText)
            _ui.hintText.text = ResolveHintText(line);

        ApplyPortraitForLine(line);
        PrepareBodyForLine(line);
    }

    private void ApplyPortraitForLine(DialogueLineData line)
    {
        if (_ui == null || _ui.portraitImage == null)
            return;

        bool shouldHidePortrait = _currentCgVisible && line.hidePortraitWhenCg;

        if (line.keepPreviousPortrait)
        {
            _ui.portraitImage.enabled = !shouldHidePortrait && _ui.portraitImage.sprite != null;
            return;
        }

        if (line.character != null)
        {
            _ui.portraitImage.sprite = line.character.Get(line.variant);
            ApplyPortraitTransform(line.character);
            _ui.portraitImage.enabled = !shouldHidePortrait && _ui.portraitImage.sprite != null;
        }
        else
        {
            _ui.portraitImage.sprite = null;
            ApplyPortraitTransform(null);
            _ui.portraitImage.enabled = false;
        }
    }

    private void PrepareBodyForLine(DialogueLineData line)
    {
        string body = GetBodyText(line);

        if (_bodyAnimator != null)
        {
            _bodyAnimator.SetContent(body, line.bodyEffects);
        }
        else if (_ui != null && _ui.bodyText != null)
        {
            _ui.bodyText.text = body ?? "";
            _ui.bodyText.maxVisibleCharacters = 0;
            _ui.bodyText.ForceMeshUpdate();
        }
    }

    private void HandleCgForLine(DialogueLineData line)
    {
        switch (line.cgMode)
        {
            case DialogueCGMode.KeepPrevious:
                return;

            case DialogueCGMode.Hide:
                StartCgTransition(null, false, line.cgFadeDuration);
                return;

            case DialogueCGMode.Show:
                if (line.cgSprite == null)
                {
                    StartCgTransition(null, false, 0f);
                    return;
                }
                StartCgTransition(line.cgSprite, true, line.cgFadeDuration);
                return;

            case DialogueCGMode.None:
            default:
                // None 直接硬清掉，不殘留
                StartCgTransition(null, false, 0f);
                return;
        }
    }

    private void StartCgTransition(Sprite sprite, bool visible, float duration)
    {
        bool nextVisible = visible && sprite != null;
        _currentCgSprite = sprite;
        _currentCgVisible = nextVisible;

        if (_cgRoutine != null)
        {
            StopCoroutine(_cgRoutine);
            _cgRoutine = null;
        }

        if (_ui == null || _ui.cgImage == null || duration <= 0f)
        {
            if (_ui != null && _ui.cgImage != null)
                _ui.cgImage.sprite = nextVisible ? sprite : null;

            SetCgAlpha(nextVisible ? 1f : 0f);

            if (!nextVisible && _ui != null && _ui.cgImage != null)
                _ui.cgImage.sprite = null;

            return;
        }

        _cgRoutine = StartCoroutine(CgTransitionRoutine(sprite, nextVisible, duration));
    }

    private IEnumerator CgTransitionRoutine(Sprite nextSprite, bool nextVisible, float duration)
    {
        if (_ui == null || _ui.cgImage == null)
        {
            _cgRoutine = null;
            yield break;
        }

        _ui.EnsureCgBehindDialogue();

        float half = Mathf.Max(0.01f, duration * 0.5f);
        bool hasCurrent = _ui.cgImage.sprite != null && _currentCgAlpha > 0.001f;

        if (hasCurrent && (_ui.cgImage.sprite != nextSprite || !nextVisible))
        {
            float t = 0f;
            float startAlpha = _currentCgAlpha;

            while (t < half)
            {
                t += Time.deltaTime;
                SetCgAlpha(Mathf.Lerp(startAlpha, 0f, t / half));
                yield return null;
            }

            SetCgAlpha(0f);
        }

        _ui.cgImage.sprite = nextVisible ? nextSprite : null;

        if (nextVisible && nextSprite != null)
        {
            float t = 0f;
            SetCgAlpha(0f);

            while (t < half)
            {
                t += Time.deltaTime;
                SetCgAlpha(Mathf.Lerp(0f, 1f, t / half));
                yield return null;
            }

            SetCgAlpha(1f);
        }
        else
        {
            SetCgAlpha(0f);
            _ui.cgImage.sprite = null;
        }

        _cgRoutine = null;
    }

    private void ApplyCurrentCgImmediate()
    {
        if (_ui == null || _ui.cgImage == null)
            return;

        _ui.EnsureCgBehindDialogue();

        _ui.cgImage.sprite = _currentCgVisible ? _currentCgSprite : null;
        SetCgAlpha(_currentCgVisible && _currentCgSprite != null ? Mathf.Max(_currentCgAlpha, 1f) : 0f);

        if (!_currentCgVisible)
            _ui.cgImage.sprite = null;
    }

    private void SetCgAlpha(float alpha)
    {
        _currentCgAlpha = Mathf.Clamp01(alpha);

        if (_ui == null || _ui.cgImage == null)
            return;

        bool show = _currentCgAlpha > 0.001f && _ui.cgImage.sprite != null;

        if (_ui.cgRoot != null)
            _ui.cgRoot.SetActive(show);

        _ui.cgImage.enabled = show;

        if (_ui.cgCanvasGroup != null)
        {
            _ui.cgCanvasGroup.alpha = _currentCgAlpha;
        }
        else
        {
            Color c = _ui.cgImage.color;
            c.a = _currentCgAlpha;
            _ui.cgImage.color = c;
        }
    }

    private void ApplyPortraitTransform(CharacterPortraitSet character)
    {
        if (_ui == null || _ui.portraitImage == null)
            return;

        RectTransform rt = _ui.portraitImage.rectTransform;

        if (!_portraitBaseCaptured)
            CapturePortraitBase();

        if (!_portraitBaseCaptured)
            return;

        RestorePortraitRect(rt);

        Vector2 offset = Vector2.zero;
        Vector3 charScale = Vector3.one;
        bool flipX = false;

        if (character != null)
        {
            offset = character.uiOffset;
            charScale = character.uiScale;
            flipX = character.flipX;
        }

        rt.anchoredPosition = _portraitBaseAnchoredPos + offset;

        float baseSign = Mathf.Sign(_portraitBaseLocalScale.x);
        if (baseSign == 0f) baseSign = 1f;

        float finalSign = flipX ? -baseSign : baseSign;

        float scaleX = Mathf.Abs(charScale.x) <= 0.0001f ? 1f : Mathf.Abs(charScale.x);
        float scaleY = Mathf.Abs(charScale.y) <= 0.0001f ? 1f : Mathf.Abs(charScale.y);
        float scaleZ = Mathf.Abs(charScale.z) <= 0.0001f ? 1f : Mathf.Abs(charScale.z);

        rt.localScale = new Vector3(finalSign * scaleX, scaleY, scaleZ);
    }

    private IEnumerator TypeLine(DialogueLineData line)
    {
        if (_ui == null || _ui.bodyText == null)
            yield break;

        int totalCharacters = GetCurrentBodyCharacterCount();
        SetVisibleCharacters(0);

        if (!useTypewriter)
        {
            SetVisibleCharacters(int.MaxValue);
            yield break;
        }

        float t = 0f;
        int visible = 0;

        while (visible < totalCharacters)
        {
            if (IsAdvancePressed())
            {
                SetVisibleCharacters(int.MaxValue);
                yield break;
            }

            t += Time.deltaTime * _activeCps;
            int newVisible = Mathf.Clamp(Mathf.FloorToInt(t), 0, totalCharacters);

            if (newVisible != visible)
            {
                visible = newVisible;
                SetVisibleCharacters(visible);
            }

            yield return null;
        }
    }

    private int GetCurrentBodyCharacterCount()
    {
        if (_bodyAnimator != null)
            return _bodyAnimator.CharacterCount;

        if (_ui == null || _ui.bodyText == null)
            return 0;

        _ui.bodyText.ForceMeshUpdate();
        return _ui.bodyText.textInfo.characterCount;
    }

    private void SetVisibleCharacters(int count)
    {
        if (_bodyAnimator != null)
        {
            _bodyAnimator.SetVisibleCharacters(count);
        }
        else if (_ui != null && _ui.bodyText != null)
        {
            _ui.bodyText.maxVisibleCharacters = count;
        }
    }

    private IEnumerator WaitUntilAdvanceReleased()
    {
        yield return null;

        while (Input.GetKey(advanceKey))
            yield return null;
    }

    private IEnumerator WaitForAdvance()
    {
        yield return null;
        while (true)
        {
            if (IsAdvancePressed())
                yield break;

            yield return null;
        }
    }

    private bool IsAdvancePressed()
    {
        if (!_isPlaying) return false;
        if (allowMouseClick && Input.GetMouseButtonDown(0)) return true;
        if (!allowSpace) return false;
        return Input.GetKeyDown(advanceKey);
    }

    private string ResolveHintText(DialogueLineData line)
    {
        switch (line.hintMode)
        {
            case HintMode.None:
                return "";

            case HintMode.Custom:
                return line.hintCustom ?? "";

            case HintMode.Default:
            default:
            {
                string keyName = advanceKey.ToString().ToUpper();
                return allowMouseClick ? $"按{keyName} / 點擊繼續" : $"按{keyName}繼續";
            }
        }
    }

    private string GetBodyText(DialogueLineData line)
    {
        if (line == null) return "";
        return line.body ?? "";
    }
}