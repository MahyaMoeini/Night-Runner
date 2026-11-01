using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Restyles the existing menu panels and adds settings / sound controls
/// without replacing UIManager button callbacks.
/// </summary>
public class MenuChrome : MonoBehaviour
{
    private static readonly Color Cyan = new Color(0.15f, 0.9f, 1f, 1f);
    private static readonly Color Purple = new Color(0.62f, 0.28f, 1f, 1f);
    private static readonly Color Navy = new Color(0.03f, 0.05f, 0.12f, 0.9f);

    private GameObject settingsRoot;
    private RectTransform mainCard;

    private void Awake()
    {
        StyleHud();
        Present("MainMenuPanel", true, true);
        Present("PausePanel", false, false);
        Present("GameOverPanel", false, false);
        EnsurePlayAgainButton();
    }

    private void EnsurePlayAgainButton()
    {
        GameObject panel = GameObject.Find("GameOverPanel");
        if (panel == null)
            return;

        Transform card = panel.transform.Find("NeonCard");
        Transform parent = card != null ? card : panel.transform;

        if (parent.Find("PlayAgainButton") != null ||
            parent.Find("PLAY AGAINButton") != null)
            return;

        UIManager ui = GetComponent<UIManager>();
        if (ui == null)
            ui = FindFirstObjectByType<UIManager>();

        Button playAgain = CreateMenuButton(
            parent,
            "PlayAgain",
            () =>
            {
                if (ui != null)
                    ui.RestartButton();
            }
        );

        RectTransform rect = playAgain.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 72f);
        rect.anchoredPosition = new Vector2(0f, -150f);
        StyleButton(playAgain);

        TMP_Text label = playAgain.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = "PLAY AGAIN";
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 32f;
        }

        playAgain.gameObject.name = "PlayAgainButton";

        // Keep Main Menu below Play Again if it was moved into the card.
        Transform mainMenu = parent.Find("MainMenuButton");
        if (mainMenu != null)
        {
            RectTransform menuRect = mainMenu.GetComponent<RectTransform>();
            if (menuRect != null)
                menuRect.anchoredPosition = new Vector2(0f, -238f);
        }
    }

    private void StyleHud()
    {
        TintText("ScoreText", Cyan);
        TintText("SpeedText", Cyan);
        TintText("BestText", new Color(0.75f, 0.8f, 1f));
        TintText("HighScoreText", new Color(0.75f, 0.8f, 1f));
        TintText("LivesText", new Color(1f, 0.45f, 0.55f));
        TintText("FuelText", new Color(0.45f, 1f, 0.6f));

        GameObject fill = GameObject.Find("FuelFill");
        if (fill != null)
        {
            Image image = fill.GetComponent<Image>();
            if (image != null)
                image.color = Cyan;
        }
    }

    private static void TintText(string objectName, Color color)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
            return;

        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text == null)
            return;

        text.color = color;
        text.fontStyle = FontStyles.Bold;
    }

    private void Present(string panelName, bool leftSide, bool addSettings)
    {
        GameObject panel = GameObject.Find(panelName);
        if (panel == null)
            return;

        Image backdrop = panel.GetComponent<Image>();
        if (backdrop != null)
        {
            backdrop.color = leftSide
                ? new Color(0.01f, 0.02f, 0.06f, 0.18f)
                : new Color(0.01f, 0.02f, 0.08f, 0.72f);
        }

        GameObject cardObject = new GameObject(
            "NeonCard",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(PanelFade)
        );

        cardObject.transform.SetParent(panel.transform, false);
        RectTransform card = cardObject.GetComponent<RectTransform>();
        if (leftSide)
        {
            card.anchorMin = new Vector2(0.04f, 0.12f);
            card.anchorMax = new Vector2(0.4f, 0.88f);
            mainCard = card;
        }
        else
        {
            card.anchorMin = new Vector2(0.3f, 0.16f);
            card.anchorMax = new Vector2(0.7f, 0.84f);
        }

        card.offsetMin = Vector2.zero;
        card.offsetMax = Vector2.zero;

        Image cardImage = cardObject.GetComponent<Image>();
        cardImage.color = Navy;
        if (backdrop != null)
            cardImage.sprite = backdrop.sprite;
        cardImage.type = Image.Type.Sliced;

        var children = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in panel.transform)
        {
            if (child != card)
                children.Add(child);
        }

        if (addSettings)
        {
            children.Insert(2, CreateMenuButton(panel.transform, "SETTINGS", OpenSettings).transform);
            children.Insert(3, CreateMenuButton(panel.transform, "SOUND", OpenSettings).transform);
            settingsRoot = CreateSettings(card);
            settingsRoot.SetActive(false);
        }

        float y = leftSide ? 280f : 210f;
        foreach (Transform child in children)
        {
            child.SetParent(card, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            Button button = child.GetComponent<Button>();
            bool isButton = button != null;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = isButton ? new Vector2(420f, 72f) : new Vector2(560f, 90f);
            rect.anchoredPosition = new Vector2(0f, y);

            TMP_Text label = child.GetComponent<TMP_Text>();
            if (label == null)
                label = child.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = isButton ? 32f : 64f;

                if (!isButton && panelName == "MainMenuPanel")
                {
                    label.text = "ENDLESS\nNIGHT";
                    label.characterSpacing = 6f;
                    label.color = Cyan;
                }

                if (isButton &&
                    panelName == "GameOverPanel" &&
                    button.gameObject.name.Contains("Restart"))
                {
                    label.text = "PLAY AGAIN";
                }
            }

            if (isButton)
            {
                StyleButton(button);
                foreach (RectTransform nested in button.GetComponentsInChildren<RectTransform>())
                {
                    if (nested == rect)
                        continue;

                    nested.anchorMin = Vector2.zero;
                    nested.anchorMax = Vector2.one;
                    nested.offsetMin = Vector2.zero;
                    nested.offsetMax = Vector2.zero;
                }
            }

            y -= isButton ? 88f : 100f;
        }
    }

    private GameObject CreateSettings(RectTransform card)
    {
        GameObject root = new GameObject("SoundSettings", typeof(RectTransform));
        root.transform.SetParent(card, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 24f);
        rect.offsetMax = new Vector2(-24f, -24f);

        CreateLabel(root.transform, "SETTINGS", 160f, 42f, Cyan);
        CreateSlider(root.transform, "MUSIC", 70f, AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.35f, SetMusic);
        CreateSlider(root.transform, "ENGINE", -10f, AudioManager.Instance != null ? AudioManager.Instance.EngineVolume : 0.55f, SetEngine);
        CreateSlider(root.transform, "EFFECTS", -90f, AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.9f, SetSfx);

        Button back = CreateMenuButton(root.transform, "BACK", CloseSettings);
        RectTransform backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0.5f);
        backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.sizeDelta = new Vector2(420f, 72f);
        backRect.anchoredPosition = new Vector2(0f, -190f);
        StyleButton(back);
        return root;
    }

    private void OpenSettings()
    {
        if (mainCard == null || settingsRoot == null)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        foreach (Transform child in mainCard)
            child.gameObject.SetActive(child.gameObject == settingsRoot);

        settingsRoot.SetActive(true);
    }

    private void CloseSettings()
    {
        if (mainCard == null || settingsRoot == null)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        settingsRoot.SetActive(false);
        foreach (Transform child in mainCard)
        {
            if (child.gameObject != settingsRoot)
                child.gameObject.SetActive(true);
        }
    }

    private static void SetMusic(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private static void SetEngine(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetEngineVolume(value);
    }

    private static void SetSfx(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSfxVolume(value);
    }

    private Button CreateMenuButton(Transform parent, string caption, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(
            caption + "Button",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        GameObject sample = GameObject.Find("StartButton");
        if (sample != null)
        {
            Image sampleImage = sample.GetComponent<Image>();
            if (sampleImage != null)
            {
                image.sprite = sampleImage.sprite;
                image.type = Image.Type.Sliced;
            }
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text existing = FindFirstObjectByType<TMP_Text>();
        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        if (existing != null)
            text.font = existing.font;

        text.text = caption;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.fontSize = 30f;
        text.raycastTarget = false;
        return button;
    }

    private static void CreateLabel(Transform parent, string caption, float y, float size, Color color)
    {
        GameObject labelObject = new GameObject(caption, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(480f, 70f);
        rect.anchoredPosition = new Vector2(0f, y);

        TMP_Text existing = Object.FindFirstObjectByType<TMP_Text>();
        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        if (existing != null)
            text.font = existing.font;

        text.text = caption;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;
    }

    private static void CreateSlider(
        Transform parent,
        string caption,
        float y,
        float startValue,
        UnityEngine.Events.UnityAction<float> changed)
    {
        CreateLabel(parent, caption, y + 28f, 22f, Color.white);

        GameObject sliderObject = new GameObject(
            caption + "Slider",
            typeof(RectTransform),
            typeof(Slider)
        );
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 28f);
        rect.anchoredPosition = new Vector2(0f, y);

        Image background = CreateImage(sliderObject.transform, "Background", new Color(0.08f, 0.1f, 0.16f, 1f));
        Stretch(background.rectTransform);
        Image fill = CreateImage(sliderObject.transform, "Fill", Cyan);
        Stretch(fill.rectTransform);
        Image handle = CreateImage(sliderObject.transform, "Handle", Color.white);
        handle.rectTransform.sizeDelta = new Vector2(20f, 32f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = startValue;
        slider.onValueChanged.AddListener(changed);
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StyleButton(Button button)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.7f, 1f, 1f, 1f);
        colors.pressedColor = Purple;
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            string name = button.gameObject.name.ToLower();
            image.color = name.Contains("exit")
                ? new Color(0.45f, 0.16f, 0.62f, 0.95f)
                : new Color(0.08f, 0.55f, 0.72f, 0.95f);
        }

        if (button.GetComponent<NeonButtonHover>() == null)
            button.gameObject.AddComponent<NeonButtonHover>();
    }
}

public class PanelFade : MonoBehaviour
{
    private CanvasGroup group;
    private float alpha;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        alpha = 0f;
        if (group != null)
            group.alpha = 0f;

        transform.localScale = Vector3.one * 0.96f;
    }

    private void Update()
    {
        alpha = Mathf.MoveTowards(alpha, 1f, Time.unscaledDeltaTime * 3.5f);
        if (group != null)
            group.alpha = alpha;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one,
            1f - Mathf.Exp(-8f * Time.unscaledDeltaTime)
        );
    }
}

public class NeonButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 baseScale = Vector3.one;
    private float target = 1f;

    private void OnEnable()
    {
        baseScale = Vector3.one;
        target = 1f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        target = 1.06f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        target = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        target = 0.96f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        target = 1.04f;
    }

    private void Update()
    {
        Vector3 goal = baseScale * target;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            goal,
            1f - Mathf.Exp(-14f * Time.unscaledDeltaTime)
        );
    }
}
