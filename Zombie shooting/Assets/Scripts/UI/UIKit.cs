using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Shared builders for every runtime menu so the whole game uses one look:
/// dark translucent panels, bone-white text, blood-red accents, SlimUI frames and click sounds.
/// Buttons are wired in code, so they never depend on scene wiring.
/// </summary>
public static class UIKit
{
    public static readonly Color Bone = AtmosphereDirector.Palette.Bone;
    public static readonly Color Blood = AtmosphereDirector.Palette.Blood;
    public static readonly Color Toxic = AtmosphereDirector.Palette.Toxic;
    public static readonly Color Dim = new Color(0.62f, 0.66f, 0.64f, 1f);
    public static readonly Color PanelColor = new Color(0.03f, 0.035f, 0.04f, 0.95f); // high alpha: the project renders in linear space
    public static readonly Color ButtonIdle = new Color(0.09f, 0.09f, 0.10f, 0.92f);

    public static UISkin Skin => UISkin.Get();

    // ------------------------------------------------------------------ infrastructure
    /// <summary>Makes sure clicks reach the UI: one active EventSystem with a working input module.</summary>
    public static void EnsureEventSystem()
    {
        var current = EventSystem.current != null ? EventSystem.current : UnityEngine.Object.FindAnyObjectByType<EventSystem>();
        if (current != null && current.isActiveAndEnabled)
        {
#if ENABLE_INPUT_SYSTEM
            // One input path for every scene: the Input System UI module (the menu scene still had the
            // legacy StandaloneInputModule, which reads a different input backend).
            var legacy = current.GetComponent<StandaloneInputModule>();
            if (legacy != null) legacy.enabled = false;
            var ism = current.GetComponent<InputSystemUIInputModule>();
            if (ism == null) ism = current.gameObject.AddComponent<InputSystemUIInputModule>();
            ism.enabled = true;
            if (ism.actionsAsset == null) ism.AssignDefaultActions();
            return;
#else
            var module = current.GetComponent<BaseInputModule>();
            if (module != null && module.enabled) return;
#endif
        }
        var go = new GameObject("EventSystem (UI)");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        if (parent != null) go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(string name, Transform parent)
        => Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

    public static Image Image(string name, Transform parent, Color color, bool raycast = false)
    {
        var img = Stretch(name, parent).gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
                                        TextAlignmentOptions align = TextAlignmentOptions.Center, bool title = false, float spacing = 4f)
    {
        var t = Stretch("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
        var font = title ? (Skin != null && Skin.titleFont != null ? Skin.titleFont : null) : (Skin != null && Skin.bodyFont != null ? Skin.bodyFont : null);
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = spacing;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 boxSize,
                                        TextAlignmentOptions align = TextAlignmentOptions.Center, bool title = false, float spacing = 4f)
    {
        var t = Label(parent, text, size, color, align, title, spacing);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = boxSize;
        return t;
    }

    /// <summary>Dark panel with a red accent line on top and the pack's corner details.</summary>
    public static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = Rect(name, parent, anchor, anchor, pivot, pos, size);
        var bg = rt.gameObject.AddComponent<Image>();
        bg.color = PanelColor;
        bg.raycastTarget = true; // panels swallow clicks so nothing behind them reacts
        var accent = Rect("Accent", rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 4)).gameObject.AddComponent<Image>();
        accent.color = Blood;
        accent.raycastTarget = false;
        if (Skin != null && Skin.cornerDetail != null)
        {
            var c1 = Rect("Corner", rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 8), new Vector2(46, 46)).gameObject.AddComponent<Image>();
            c1.sprite = Skin.cornerDetail; c1.color = new Color(Bone.r, Bone.g, Bone.b, 0.35f); c1.raycastTarget = false;
            var c2 = Rect("Corner", rt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(46, 46)).gameObject.AddComponent<Image>();
            c2.sprite = Skin.cornerDetail; c2.color = c1.color; c2.raycastTarget = false;
            c2.rectTransform.localScale = new Vector3(-1, 1, 1);
        }
        return rt;
    }

    public static void Divider(Transform parent, Vector2 anchor, Vector2 pos, float width)
    {
        var img = Rect("Divider", parent, anchor, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(width, 2)).gameObject.AddComponent<Image>();
        img.color = new Color(Bone.r, Bone.g, Bone.b, 0.18f);
        img.raycastTarget = false;
    }

    // ------------------------------------------------------------------ controls
    public static Button Button(Transform parent, string text, Action onClick, Vector2 anchor, Vector2 pos, Vector2 size, bool primary = false)
    {
        var rt = Rect("Btn " + text, parent, anchor, anchor, new Vector2(0.5f, 0.5f), pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = primary ? new Color(Blood.r * 0.85f, Blood.g, Blood.b, 0.95f) : ButtonIdle;
        img.raycastTarget = true;

        // SlimUI frame drawn over the fill
        if (Skin != null && Skin.buttonFrame != null)
        {
            var frame = Stretch("Frame", rt).gameObject.AddComponent<Image>();
            frame.sprite = Skin.buttonFrame;
            frame.color = new Color(Bone.r, Bone.g, Bone.b, 0.22f);
            frame.raycastTarget = false;
        }

        var label = Label(rt, text, Mathf.Min(30f, size.y * 0.45f), Bone, TextAlignmentOptions.Center, false, 7f);
        label.name = "Text";

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = primary ? new Color(1.25f, 1.25f, 1.25f) : new Color(2.0f, 0.55f, 0.55f);
        cb.selectedColor = Color.white;
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        cb.colorMultiplier = 1.6f;
        cb.fadeDuration = 0.08f;
        button.colors = cb;
        var nav = button.navigation; nav.mode = Navigation.Mode.Vertical; button.navigation = nav;
        button.onClick.AddListener(() => { PlayClick(); onClick?.Invoke(); });
        rt.gameObject.AddComponent<UIButtonFeel>();
        return button;
    }

    public static Slider Slider(Transform parent, string text, float min, float max, float value, Action<float> onChange, Vector2 anchor, Vector2 pos, float width, Func<float, string> format = null)
    {
        var row = Rect("Slider " + text, parent, anchor, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(width, 64));
        Label(row, text, 22, Dim, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(width * 0.6f, 28), TextAlignmentOptions.Left, false, 5);
        var valueText = Label(row, "", 22, Bone, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(width * 0.4f, 28), TextAlignmentOptions.Right, false, 2);

        var track = Rect("Track", row, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(0, 10));
        var trackImg = track.gameObject.AddComponent<Image>(); trackImg.color = new Color(0, 0, 0, 0.6f);
        var fillArea = Rect("Fill Area", track, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var fill = Rect("Fill", fillArea, Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        var fillImg = fill.gameObject.AddComponent<Image>(); fillImg.color = Blood; fillImg.raycastTarget = false;
        var handleArea = Rect("Handle Area", track, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16, 0));
        var handle = Rect("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 26));
        var handleImg = handle.gameObject.AddComponent<Image>(); handleImg.color = Bone;

        var s = track.gameObject.AddComponent<Slider>();
        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = handleImg;
        s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        s.minValue = min;
        s.maxValue = max;
        s.value = value;
        format ??= v => v.ToString("0.00");
        valueText.text = format(value);
        s.onValueChanged.AddListener(v => { valueText.text = format(v); onChange?.Invoke(v); });
        return s;
    }

    public static Toggle Toggle(Transform parent, string text, bool value, Action<bool> onChange, Vector2 anchor, Vector2 pos, float width)
    {
        var row = Rect("Toggle " + text, parent, anchor, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(width, 44));
        var rowImg = row.gameObject.AddComponent<Image>(); rowImg.color = new Color(0, 0, 0, 0.001f); // whole row clickable
        Label(row, text, 22, Dim, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(width - 80, 40), TextAlignmentOptions.Left, false, 5);
        var box = Rect("Box", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(64, 30));
        var boxImg = box.gameObject.AddComponent<Image>(); boxImg.color = new Color(0, 0, 0, 0.6f);
        var knob = Rect("Knob", box, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(26, 22));
        var knobImg = knob.gameObject.AddComponent<Image>(); knobImg.color = Bone;
        var t = row.gameObject.AddComponent<Toggle>();
        t.targetGraphic = rowImg;
        t.isOn = value;
        void Paint(bool on)
        {
            boxImg.color = on ? new Color(Blood.r, Blood.g, Blood.b, 0.9f) : new Color(0, 0, 0, 0.6f);
            knob.anchoredPosition = new Vector2(on ? 34 : 4, 0);
        }
        Paint(value);
        t.onValueChanged.AddListener(on => { Paint(on); PlayClick(); onChange?.Invoke(on); });
        return t;
    }

    // ------------------------------------------------------------------ audio
    private static AudioSource uiAudio;
    private static AudioSource UIAudio
    {
        get
        {
            if (uiAudio == null)
            {
                var go = new GameObject("UI Audio");
                UnityEngine.Object.DontDestroyOnLoad(go);
                uiAudio = go.AddComponent<AudioSource>();
                uiAudio.playOnAwake = false;
                uiAudio.ignoreListenerPause = true;   // menus still click while the game is paused
                uiAudio.spatialBlend = 0f;
            }
            return uiAudio;
        }
    }

    public static void PlayClick() { if (Skin != null && Skin.clickSound != null) UIAudio.PlayOneShot(Skin.clickSound, 0.8f); }
    public static void PlayHover() { if (Skin != null && Skin.hoverSound != null) UIAudio.PlayOneShot(Skin.hoverSound, 0.35f); }

    // ------------------------------------------------------------------ settings shared by menus
    public const string PrefVolume = "MasterVolume";
    public const string PrefInvertY = "InvertY";

    /// <summary>Settings panel content (sensitivity, volume, invert Y, fullscreen). Returns the panel.</summary>
    public static RectTransform BuildSettingsPanel(Transform parent, Action onBack)
    {
        var p = Panel("Settings", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 560));
        Label(p, "SETTINGS", 46, Bone, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(680, 60), TextAlignmentOptions.Center, true, 14);
        Divider(p, new Vector2(0.5f, 1), new Vector2(0, -104), 600);
        Slider(p, "MOUSE SENSITIVITY", 0.2f, 3f, PlayerPrefs.GetFloat(MouseLook.SensitivityPrefKey, 1f),
               v => { PlayerPrefs.SetFloat(MouseLook.SensitivityPrefKey, v); PlayerPrefs.Save(); },
               new Vector2(0.5f, 1), new Vector2(0, -160), 600, v => v.ToString("0.0") + "x");
        Slider(p, "VOLUME", 0f, 1f, PlayerPrefs.GetFloat(PrefVolume, 1f),
               v => { PlayerPrefs.SetFloat(PrefVolume, v); PlayerPrefs.Save(); AudioListener.volume = v; },
               new Vector2(0.5f, 1), new Vector2(0, -250), 600, v => Mathf.RoundToInt(v * 100) + "%");
        Toggle(p, "INVERT MOUSE Y", PlayerPrefs.GetInt(PrefInvertY, 0) == 1,
               on => { PlayerPrefs.SetInt(PrefInvertY, on ? 1 : 0); PlayerPrefs.Save(); },
               new Vector2(0.5f, 1), new Vector2(0, -330), 600);
        Toggle(p, "FULLSCREEN", Screen.fullScreen, on => Screen.fullScreen = on,
               new Vector2(0.5f, 1), new Vector2(0, -390), 600);
        Button(p, "BACK", onBack, new Vector2(0.5f, 0), new Vector2(0, 64), new Vector2(320, 64));
        return p;
    }
}
