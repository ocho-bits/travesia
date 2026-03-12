using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseSettingsOverlay : MonoBehaviour
{
    private const string UiRootName = "__PauseSettingsUI";
    private const string MainMenuSceneName = "MainMenu";

    private const string SoundKey = "menu.sound";
    private const string MusicKey = "menu.music";
    private const string BrightnessKey = "menu.brightness";
    private const string ContrastKey = "menu.contrast";
    private const string LanguageKey = "menu.language";

    private static PauseSettingsOverlay _instance;

    private readonly string[] _languages = { "English", "Spanish", "French", "German" };

    private TMP_FontAsset _tmpFont;
    private Sprite _whiteSprite;

    private GameObject _uiRoot;
    private GameObject _overlay;
    private GameObject _panel;
    private TMP_Text _languageText;
    private bool _isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHooks()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMainMenu = string.Equals(scene.name, MainMenuSceneName, StringComparison.OrdinalIgnoreCase);
        if (isMainMenu)
        {
            if (_instance != null)
            {
                _instance.Close();
                _instance.enabled = false;
            }

            return;
        }

        if (_instance == null)
        {
            GameObject go = new GameObject("PauseSettingsOverlay");
            _instance = go.AddComponent<PauseSettingsOverlay>();
            DontDestroyOnLoad(go);
        }

        _instance.HandleGameplaySceneLoaded();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!enabled)
        {
            return;
        }

        if (IsEscapePressed())
        {
            Toggle();
        }
    }

    private void HandleGameplaySceneLoaded()
    {
        enabled = true;

        EnsureEventSystem();
        EnsureUI();
        _uiRoot.SetActive(true);

        if (_overlay != null)
        {
            _overlay.SetActive(false);
        }

        if (_panel != null)
        {
            _panel.SetActive(false);
        }

        _isOpen = false;
        Time.timeScale = 1f;
        ApplyCurrentSettingsToRuntime();
    }

    private void EnsureUI()
    {
        if (_uiRoot != null)
        {
            return;
        }

        _tmpFont = TMP_Settings.defaultFontAsset;
        if (_tmpFont == null)
        {
            _tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        _uiRoot = new GameObject(UiRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(_uiRoot);

        Canvas canvas = _uiRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = _uiRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = _uiRoot.GetComponent<RectTransform>();

        Image overlayImage = CreateImage("Overlay", root, new Color(0f, 0f, 0f, 0.45f));
        Stretch(overlayImage.rectTransform);
        _overlay = overlayImage.gameObject;

        _panel = CreatePanel("SettingsPanel", root, new Vector2(1300f, 760f), Color.white);

        TMP_Text header = CreateText("Header", _panel.transform as RectTransform, "Settings", 52, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(500f, 90f));
        header.rectTransform.anchoredPosition = new Vector2(0f, 300f);

        CreateSliderRow(_panel.transform as RectTransform, "Sound Volume", SoundKey, new Vector2(0f, 170f));
        CreateSliderRow(_panel.transform as RectTransform, "Music Volume", MusicKey, new Vector2(0f, 80f));
        CreateSliderRow(_panel.transform as RectTransform, "Brightness", BrightnessKey, new Vector2(0f, -10f));
        CreateSliderRow(_panel.transform as RectTransform, "Contrast", ContrastKey, new Vector2(0f, -100f));
        CreateLanguageRow(_panel.transform as RectTransform, new Vector2(0f, -190f));

        Button resume = CreateButton("Resume", _panel.transform as RectTransform, new Vector2(260f, 80f), Close);
        resume.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -300f);

        _overlay.SetActive(false);
        _panel.SetActive(false);
    }

    private void Toggle()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        if (_uiRoot == null || _panel == null || _overlay == null)
        {
            EnsureUI();
        }

        EnsureEventSystem();
        ApplyCurrentSettingsToRuntime();

        _overlay.SetActive(true);
        _panel.SetActive(true);
        _isOpen = true;
        Time.timeScale = 0f;
    }

    private void Close()
    {
        if (_overlay != null)
        {
            _overlay.SetActive(false);
        }

        if (_panel != null)
        {
            _panel.SetActive(false);
        }

        if (_uiRoot != null && string.Equals(SceneManager.GetActiveScene().name, MainMenuSceneName, StringComparison.OrdinalIgnoreCase))
        {
            _uiRoot.SetActive(false);
        }

        _isOpen = false;
        Time.timeScale = 1f;
    }

    private static bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

        return Input.GetKeyDown(KeyCode.Escape);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();

        Type inputSystemType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemType != null)
        {
            go.AddComponent(inputSystemType);
        }
        else
        {
            go.AddComponent<StandaloneInputModule>();
        }
    }

    private void CreateSliderRow(RectTransform parent, string label, string key, Vector2 position)
    {
        GameObject row = new GameObject(label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(960f, 70f);
        rowRt.anchoredPosition = position;

        TMP_Text t = CreateText("Label", rowRt, label, 28, FontStyles.Normal, TextAlignmentOptions.Left, new Vector2(300f, 50f));
        t.rectTransform.anchoredPosition = new Vector2(-300f, 0f);

        Slider slider = CreateSlider("Slider", rowRt, new Vector2(520f, 40f));
        slider.GetComponent<RectTransform>().anchoredPosition = new Vector2(190f, 0f);
        slider.value = PlayerPrefs.GetFloat(key, 0.8f);
        slider.onValueChanged.AddListener(v =>
        {
            PlayerPrefs.SetFloat(key, v);
            PlayerPrefs.Save();

            if (key == SoundKey)
            {
                AudioListener.volume = v;
            }
        });
    }

    private void CreateLanguageRow(RectTransform parent, Vector2 position)
    {
        GameObject row = new GameObject("Language", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(960f, 70f);
        rowRt.anchoredPosition = position;

        TMP_Text label = CreateText("Label", rowRt, "Language", 28, FontStyles.Normal, TextAlignmentOptions.Left, new Vector2(300f, 50f));
        label.rectTransform.anchoredPosition = new Vector2(-300f, 0f);

        Button prev = CreateButton("<", rowRt, new Vector2(70f, 56f), PreviousLanguage);
        prev.GetComponent<RectTransform>().anchoredPosition = new Vector2(60f, 0f);

        _languageText = CreateText("LanguageValue", rowRt, "English", 26, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(220f, 50f));
        _languageText.rectTransform.anchoredPosition = new Vector2(200f, 0f);

        Button next = CreateButton(">", rowRt, new Vector2(70f, 56f), NextLanguage);
        next.GetComponent<RectTransform>().anchoredPosition = new Vector2(340f, 0f);
    }

    private void ApplyCurrentSettingsToRuntime()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(SoundKey, 0.8f);

        int idx = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, _languages.Length - 1);
        if (_languageText != null)
        {
            _languageText.text = _languages[idx];
        }
    }

    private void PreviousLanguage()
    {
        ChangeLanguage(-1);
    }

    private void NextLanguage()
    {
        ChangeLanguage(1);
    }

    private void ChangeLanguage(int delta)
    {
        int idx = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, _languages.Length - 1);
        idx = (idx + delta + _languages.Length) % _languages.Length;
        PlayerPrefs.SetInt(LanguageKey, idx);
        PlayerPrefs.Save();

        if (_languageText != null)
        {
            _languageText.text = _languages[idx];
        }
    }

    private GameObject CreatePanel(string name, RectTransform parent, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.sprite = _whiteSprite;
        img.color = color;

        return go;
    }

    private Image CreateImage(string name, RectTransform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = _whiteSprite;
        img.color = color;
        return img;
    }

    private Button CreateButton(string text, RectTransform parent, Vector2 size, Action action)
    {
        Image img = CreateImage(text + "Button", parent, new Color(0.7f, 0.7f, 0.7f, 1f));
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        Button b = img.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.highlightedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.pressedColor = new Color(0.58f, 0.58f, 0.58f, 1f);
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        b.colors = cb;

        TMP_Text t = CreateText("Text", rt, text, 28, FontStyles.Normal, TextAlignmentOptions.Center, size);
        t.raycastTarget = false;

        if (action != null)
        {
            b.onClick.AddListener(() => action());
        }

        return b;
    }

    private TMP_Text CreateText(string name, RectTransform parent, string content, int fontSize, FontStyles style, TextAlignmentOptions align, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (_tmpFont != null)
        {
            t.font = _tmpFont;
        }

        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = Color.black;
        t.alignment = align;
        return t;
    }

    private Slider CreateSlider(string name, RectTransform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        Slider s = go.GetComponent<Slider>();
        s.minValue = 0f;
        s.maxValue = 1f;

        Image bg = CreateImage("Background", rt, new Color(0.85f, 0.85f, 0.85f, 1f));
        Stretch(bg.rectTransform);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(rt, false);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRt, 10f, 10f, 14f, 14f);

        Image fill = CreateImage("Fill", fillAreaRt, new Color(0.55f, 0.55f, 0.55f, 1f));
        Stretch(fill.rectTransform);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(rt, false);
        RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRt, 10f, 10f, 10f, 10f);

        Image handle = CreateImage("Handle", handleAreaRt, new Color(0.35f, 0.35f, 0.35f, 1f));
        handle.rectTransform.sizeDelta = new Vector2(24f, 44f);

        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;

        return s;
    }

    private static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.anchoredPosition = Vector2.zero;
    }
}
