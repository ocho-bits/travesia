using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenuBootstrap : MonoBehaviour
{
    [Header("Scene Routing")]
    [SerializeField] private string playSceneName = "PixelTest";
    [SerializeField] private string[] trackSceneNames = new string[9] { "PixelTest", "", "", "", "", "", "", "", "" };

    private readonly string[] _languages = { "English", "Spanish", "French", "German" };

    private const string RootName = "__MainMenuUI";
    private const string SoundKey = "menu.sound";
    private const string MusicKey = "menu.music";
    private const string BrightnessKey = "menu.brightness";
    private const string ContrastKey = "menu.contrast";
    private const string LanguageKey = "menu.language";

    private TMP_FontAsset _tmpFont;
    private Sprite _whiteSprite;

    private GameObject _menuPanel;
    private GameObject _tracksPanel;
    private GameObject _settingsPanel;

    private TMP_Text _languageText;

    private void OnEnable()
    {
        EnsureSceneSetup();
    }

    private void OnValidate()
    {
        if (trackSceneNames == null || trackSceneNames.Length != 9)
        {
            trackSceneNames = new string[9];
        }

        if (string.IsNullOrWhiteSpace(trackSceneNames[0]))
        {
            trackSceneNames[0] = playSceneName;
        }
    }

    private void EnsureSceneSetup()
    {
        EnsureCamera();
        EnsureEventSystem();
        BuildOrRefreshUI();
        ApplySettings();
    }

    private void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.white;
        cam.orthographic = true;
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

    private void BuildOrRefreshUI()
    {
        _tmpFont = TMP_Settings.defaultFontAsset;
        if (_tmpFont == null)
        {
            _tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existing);
            }
            else
            {
                DestroyImmediate(existing);
            }
        }

        GameObject canvasGo = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasGo.GetComponent<RectTransform>();

        Image bg = CreateImage("Background", root, Color.white);
        Stretch(bg.rectTransform);

        _menuPanel = CreatePanel("MenuPanel", root, new Vector2(560f, 620f), Color.white);
        VerticalLayoutGroup v = _menuPanel.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 22f;
        v.childControlHeight = false;
        v.childControlWidth = false;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = false;
        ContentSizeFitter f = _menuPanel.AddComponent<ContentSizeFitter>();
        f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText("Title", _menuPanel.transform as RectTransform, "TRAVESIA", 56, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(460f, 100f));
        CreateButton("Play", _menuPanel.transform as RectTransform, new Vector2(420f, 84f), StartPlay);
        CreateButton("Tracks", _menuPanel.transform as RectTransform, new Vector2(420f, 84f), ShowTracks);
        CreateButton("Settings", _menuPanel.transform as RectTransform, new Vector2(420f, 84f), ShowSettings);
        CreateButton("Exit", _menuPanel.transform as RectTransform, new Vector2(420f, 84f), ExitGame);

        _tracksPanel = BuildTracksPanel(root);
        _settingsPanel = BuildSettingsPanel(root);

        BackToMenu();
    }

    private GameObject BuildTracksPanel(RectTransform root)
    {
        GameObject panel = new GameObject("TracksPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false);
        Image overlay = panel.GetComponent<Image>();
        overlay.sprite = _whiteSprite;
        overlay.color = new Color(1f, 1f, 1f, 0.98f);
        Stretch(panel.GetComponent<RectTransform>());

        GameObject content = CreatePanel("TracksContent", panel.transform as RectTransform, new Vector2(1300f, 760f), Color.white);
        RectTransform contentRt = content.GetComponent<RectTransform>();

        TMP_Text header = CreateText("TracksHeader", contentRt, "Tracks", 52, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(500f, 90f));
        header.rectTransform.anchoredPosition = new Vector2(0f, 300f);

        GameObject gridObj = new GameObject("TrackGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObj.transform.SetParent(contentRt, false);
        RectTransform gridRt = gridObj.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.5f, 0.5f);
        gridRt.anchorMax = new Vector2(0.5f, 0.5f);
        gridRt.pivot = new Vector2(0.5f, 0.5f);
        gridRt.sizeDelta = new Vector2(1080f, 430f);
        gridRt.anchoredPosition = new Vector2(0f, 20f);

        GridLayoutGroup grid = gridObj.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(320f, 100f);
        grid.spacing = new Vector2(30f, 30f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 9; i++)
        {
            int index = i;
            string sceneName = (trackSceneNames != null && i < trackSceneNames.Length) ? trackSceneNames[i] : string.Empty;
            bool assigned = !string.IsNullOrWhiteSpace(sceneName);
            string label = assigned ? $"Track {i + 1}" : $"Track {i + 1} (Unassigned)";
            Button b = CreateButton(label, gridRt, new Vector2(320f, 100f), () => LoadTrack(index));
            b.interactable = assigned;
        }

        Button back = CreateButton("Back", contentRt, new Vector2(260f, 80f), BackToMenu);
        back.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -300f);

        return panel;
    }

    private GameObject BuildSettingsPanel(RectTransform root)
    {
        GameObject panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false);
        Image overlay = panel.GetComponent<Image>();
        overlay.sprite = _whiteSprite;
        overlay.color = new Color(1f, 1f, 1f, 0.98f);
        Stretch(panel.GetComponent<RectTransform>());

        GameObject content = CreatePanel("SettingsContent", panel.transform as RectTransform, new Vector2(1300f, 760f), Color.white);
        RectTransform contentRt = content.GetComponent<RectTransform>();

        TMP_Text header = CreateText("SettingsHeader", contentRt, "Settings", 52, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(500f, 90f));
        header.rectTransform.anchoredPosition = new Vector2(0f, 300f);

        CreateSliderRow(contentRt, "Sound Volume", SoundKey, new Vector2(0f, 170f));
        CreateSliderRow(contentRt, "Music Volume", MusicKey, new Vector2(0f, 80f));
        CreateSliderRow(contentRt, "Brightness", BrightnessKey, new Vector2(0f, -10f));
        CreateSliderRow(contentRt, "Contrast", ContrastKey, new Vector2(0f, -100f));
        CreateLanguageRow(contentRt, new Vector2(0f, -190f));

        Button back = CreateButton("Back", contentRt, new Vector2(260f, 80f), BackToMenu);
        back.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -300f);

        return panel;
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

    private void StartPlay()
    {
        LoadScene(playSceneName);
    }

    private void LoadTrack(int index)
    {
        if (trackSceneNames == null || index < 0 || index >= trackSceneNames.Length)
        {
            return;
        }

        LoadScene(trackSceneNames[index]);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Scene not in Build Settings: " + sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void ShowTracks()
    {
        _menuPanel.SetActive(false);
        _tracksPanel.SetActive(true);
        _settingsPanel.SetActive(false);
    }

    private void ShowSettings()
    {
        _menuPanel.SetActive(false);
        _tracksPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    private void BackToMenu()
    {
        if (_menuPanel != null) _menuPanel.SetActive(true);
        if (_tracksPanel != null) _tracksPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }

    private void ApplySettings()
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

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
