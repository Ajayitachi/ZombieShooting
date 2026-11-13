using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu built at runtime (replaces the broken SlimUI demo menu, which is hidden):
/// title, PLAY / SETTINGS / CONTROLS / QUIT, a loading screen, and menu music.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    public string gameTitle = "ZOMBIE SHOOTING";
    public string tagline = "SURVIVE THE HORDE AT THE OLD SEA PORT";
    public string gameScene = "Map1";

    private RectTransform mainColumn, settingsPanel, controlsPanel, levelsPanel, loadingPanel;
    private TextMeshProUGUI loadingText;
    private Image loadingFill;
    private RawImage backdrop;
    private AudioSource music;

    private void Start()
    {
        Time.timeScale = 1f;
        GameState.ResetMatch();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        HideOldMenus();
        UIKit.EnsureEventSystem();
        Build();
        PlayMusic();
    }

    // The SlimUI demo menu never shows up (broken panel animations / references) — hide all of it.
    private void HideOldMenus()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            if (c.isRootCanvas && !c.transform.IsChildOf(transform))
                c.gameObject.SetActive(false);
        foreach (var src in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
            if (src.playOnAwake && src.isPlaying) src.Stop();
    }

    private void Build()
    {
        var canvas = UIKit.CreateCanvas("Main Menu", 50, transform);
        var root = canvas.transform;

        // Backdrop: a shot of the game's own harbour (falls back to a dark gradient).
        var skin = UIKit.Skin;
        var bgRect = UIKit.Stretch("Backdrop", root);
        bgRect.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.025f, 0.03f, 1f);
        if (skin != null && skin.menuBackground != null)
        {
            backdrop = UIKit.Stretch("Backdrop Image", root).gameObject.AddComponent<RawImage>();
            backdrop.texture = skin.menuBackground;
            backdrop.raycastTarget = false;
            var fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)skin.menuBackground.width / skin.menuBackground.height;
        }
        // Darken the left side where the menu sits, plus a vignette.
        var shade = UIKit.Image("Shade", root, new Color(0, 0, 0, 0));
        shade.sprite = MakeGradientSprite();
        shade.type = Image.Type.Simple;
        shade.color = new Color(0, 0, 0, 0.88f);

        // Left column: title + buttons
        mainColumn = UIKit.Rect("Main", root, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(140, 0), new Vector2(720, 860));
        var accent = UIKit.Rect("Accent", mainColumn, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -40), new Vector2(120, 6)).gameObject.AddComponent<Image>();
        accent.color = UIKit.Blood;
        UIKit.Label(mainColumn, gameTitle, 104, UIKit.Bone, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -60), new Vector2(1100, 130), TextAlignmentOptions.Left, true, 6);
        UIKit.Label(mainColumn, tagline, 24, UIKit.Dim, new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -196), new Vector2(900, 34), TextAlignmentOptions.Left, false, 10);

        float y = -330f;
        UIKit.Button(mainColumn, "PLAY", Play, new Vector2(0, 1), new Vector2(190, y), new Vector2(380, 78), true); y -= 100;
        UIKit.Button(mainColumn, "SELECT LEVEL", () => Show(levelsPanel), new Vector2(0, 1), new Vector2(190, y), new Vector2(380, 70)); y -= 88;
        UIKit.Button(mainColumn, "SETTINGS", () => Show(settingsPanel), new Vector2(0, 1), new Vector2(190, y), new Vector2(380, 70)); y -= 88;
        UIKit.Button(mainColumn, "CONTROLS", () => Show(controlsPanel), new Vector2(0, 1), new Vector2(190, y), new Vector2(380, 70)); y -= 88;
        UIKit.Button(mainColumn, "QUIT", Quit, new Vector2(0, 1), new Vector2(190, y), new Vector2(380, 70));

        UIKit.Label(root, "v1.0  ·  ESC TO PAUSE IN GAME", 18, new Color(1, 1, 1, 0.35f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(600, 30), TextAlignmentOptions.Right, false, 4);

        // Right side panels
        var panels = UIKit.Rect("Panels", root, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-140, -70), new Vector2(720, 600));
        settingsPanel = UIKit.BuildSettingsPanel(panels, () => Show(null));
        controlsPanel = BuildControls(panels);
        levelsPanel = BuildLevels(panels);
        settingsPanel.gameObject.SetActive(false);
        controlsPanel.gameObject.SetActive(false);
        levelsPanel.gameObject.SetActive(false);

        // Loading overlay
        loadingPanel = UIKit.Stretch("Loading", root);
        loadingPanel.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.01f, 0.015f, 0.97f);
        loadingText = UIKit.Label(loadingPanel, "LOADING", 40, UIKit.Bone, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(800, 60), TextAlignmentOptions.Center, true, 16);
        var barBg = UIKit.Rect("Bar", loadingPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(520, 6));
        barBg.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.12f);
        loadingFill = UIKit.Rect("Fill", barBg, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 6)).gameObject.AddComponent<Image>();
        loadingFill.color = UIKit.Blood;
        loadingPanel.gameObject.SetActive(false);
    }

    private RectTransform BuildControls(Transform parent)
    {
        var p = UIKit.Panel("Controls", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 560));
        UIKit.Label(p, "CONTROLS", 46, UIKit.Bone, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(680, 60), TextAlignmentOptions.Center, true, 14);
        UIKit.Divider(p, new Vector2(0.5f, 1), new Vector2(0, -104), 600);
        string[,] rows = { { "MOVE", "W A S D" }, { "LOOK", "MOUSE" }, { "SHOOT", "LEFT MOUSE" }, { "RELOAD", "R" }, { "SPRINT", "LEFT SHIFT" }, { "JUMP", "SPACE" }, { "PAUSE", "ESC" } };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = -140 - i * 46;
            UIKit.Label(p, rows[i, 0], 22, UIKit.Dim, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(-300, y), new Vector2(300, 36), TextAlignmentOptions.Left, false, 6);
            UIKit.Label(p, rows[i, 1], 22, UIKit.Bone, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(300, y), new Vector2(300, 36), TextAlignmentOptions.Right, false, 4);
        }
        UIKit.Button(p, "BACK", () => Show(null), new Vector2(0.5f, 0), new Vector2(0, 64), new Vector2(320, 64));
        return p;
    }

    /// <summary>Every scene in Build Settings except menu scenes, as a list of level buttons.</summary>
    private RectTransform BuildLevels(Transform parent)
    {
        var p = UIKit.Panel("Levels", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 560));
        UIKit.Label(p, "SELECT LEVEL", 46, UIKit.Bone, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(680, 60), TextAlignmentOptions.Center, true, 14);
        UIKit.Divider(p, new Vector2(0.5f, 1), new Vector2(0, -104), 600);

        // Scrollable list so any number of scenes fits.
        var view = UIKit.Rect("List", p, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -124), new Vector2(620, 300));
        view.gameObject.AddComponent<RectMask2D>();
        var viewImg = view.gameObject.AddComponent<Image>(); viewImg.color = new Color(0, 0, 0, 0.001f);
        var content = UIKit.Rect("Content", view, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(620, 0));
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30f;

        int count = 0;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (sceneName.ToLowerInvariant().Contains("menu")) continue;   // don't list menus as levels
            string label = PrettyName(sceneName);
            string toLoad = sceneName;
            UIKit.Button(content, label, () => StartCoroutine(LoadGame(toLoad)), new Vector2(0.5f, 1), new Vector2(0, -40 - count * 84), new Vector2(600, 70), count == 0);
            count++;
        }
        content.sizeDelta = new Vector2(620, Mathf.Max(300, count * 84));
        if (count == 0)
            UIKit.Label(p, "NO LEVELS IN BUILD SETTINGS", 22, UIKit.Dim, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(600, 40));
        UIKit.Label(p, count + (count == 1 ? " LEVEL" : " LEVELS") + "  ·  ADD SCENES IN BUILD SETTINGS TO LIST MORE", 16, UIKit.Dim,
                    new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(680, 24));
        UIKit.Button(p, "BACK", () => Show(null), new Vector2(0.5f, 0), new Vector2(0, 64), new Vector2(320, 64));
        return p;
    }

    // "Map1" -> "MAP 1", "harbour_night" -> "HARBOUR NIGHT"
    private static string PrettyName(string sceneName)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < sceneName.Length; i++)
        {
            char c = sceneName[i];
            if (c == '_' || c == '-') { sb.Append(' '); continue; }
            if (i > 0 && (char.IsDigit(c) && !char.IsDigit(sceneName[i - 1]) || char.IsUpper(c) && char.IsLower(sceneName[i - 1]))) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString().ToUpperInvariant();
    }

    private void Show(RectTransform panel)
    {
        settingsPanel.gameObject.SetActive(panel == settingsPanel);
        controlsPanel.gameObject.SetActive(panel == controlsPanel);
        levelsPanel.gameObject.SetActive(panel == levelsPanel);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Show(null);
        // Slow drift of the backdrop so the menu feels alive.
        if (backdrop != null)
        {
            float t = Time.unscaledTime * 0.03f;
            backdrop.uvRect = new Rect(0.02f + Mathf.Sin(t) * 0.02f, 0.02f, 0.96f, 0.96f);
        }
    }

    private void Play()
    {
        string scene = Application.CanStreamedLevelBeLoaded(gameScene)
            ? gameScene
            : System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(1));
        StartCoroutine(LoadGame(scene));
    }

    private IEnumerator LoadGame(string scene)
    {
        loadingPanel.gameObject.SetActive(true);
        loadingPanel.SetAsLastSibling();
        var op = SceneManager.LoadSceneAsync(string.IsNullOrEmpty(scene) ? gameScene : scene);
        while (op != null && !op.isDone)
        {
            loadingFill.rectTransform.sizeDelta = new Vector2(520f * Mathf.Clamp01(op.progress / 0.9f), 6f);
            loadingText.text = "LOADING" + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
            if (music != null) music.volume = Mathf.MoveTowards(music.volume, 0f, Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayMusic()
    {
        var skin = UIKit.Skin;
        if (skin == null || skin.menuMusic == null) return;
        music = gameObject.AddComponent<AudioSource>();
        music.clip = skin.menuMusic;
        music.loop = true;
        music.volume = 0.35f;
        music.spatialBlend = 0f;
        music.Play();
    }

    // Horizontal gradient: opaque on the left (behind the menu), clear on the right.
    private static Sprite MakeGradientSprite()
    {
        var tex = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < 256; x++)
        {
            float a = Mathf.Clamp01(1.15f - x / 190f);
            tex.SetPixel(x, 0, new Color(1, 1, 1, a * a * (3 - 2 * a)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 256, 1), new Vector2(0.5f, 0.5f));
    }
}
