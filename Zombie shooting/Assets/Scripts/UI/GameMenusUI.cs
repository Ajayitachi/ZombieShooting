using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// In-game screens built at runtime: PAUSE (resume / settings / restart / main menu),
/// SETTINGS, YOU DIED and MISSION COMPLETE, all with buttons wired in code.
/// The scene's old pause / death / mission panels keep running (their scripts drive pausing and
/// game over) but are made invisible and click-through so they can't block these menus.
/// </summary>
public class GameMenusUI : MonoBehaviour
{
    private enum Screen { None, Pause, Settings, Death, Victory }

    public string mainMenuScene = "MainMenu";

    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform dim, pausePanel, settingsPanel, deathPanel, victoryPanel;
    private TextMeshProUGUI deathStats, victoryStats;
    private Screen current = Screen.None;
    private bool settingsFromPause;
    private float overTimer;
    private PlayerController player;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        UIKit.EnsureEventSystem();
        HideOldScreens();
        Build();
        SetScreen(Screen.None);
    }

    // ------------------------------------------------------------------ old panels
    private void HideOldScreens()
    {
        var pause = FindAnyObjectByType<PauseManager>();
        if (pause != null && pause.pauseMenu != null) MakeInvisible(pause.pauseMenu);
        var death = FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
        if (death != null) MakeInvisible(death.gameObject);
        var mission = FindAnyObjectByType<MissionCompleteUI>(FindObjectsInactive.Include);
        if (mission != null && mission.missionCompletePanel != null) MakeInvisible(mission.missionCompletePanel);
    }

    private static void MakeInvisible(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.ignoreParentGroups = true;
    }

    // ------------------------------------------------------------------ build
    private void Build()
    {
        canvas = UIKit.CreateCanvas("Game Menus", 100, transform);
        group = canvas.GetComponent<CanvasGroup>();
        var root = canvas.transform;

        dim = UIKit.Stretch("Dim", root);
        var dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0.01f, 0.01f, 0.015f, 0.9f); // linear colour space: needs ~0.9 to read as a strong dim
        dimImg.raycastTarget = true;

        // PAUSE
        pausePanel = UIKit.Panel("Pause", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 600));
        UIKit.Label(pausePanel, "PAUSED", 60, UIKit.Bone, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(520, 76), TextAlignmentOptions.Center, true, 18);
        UIKit.Divider(pausePanel, new Vector2(0.5f, 1), new Vector2(0, -130), 440);
        float y = -200;
        UIKit.Button(pausePanel, "RESUME", Resume, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(400, 72), true); y -= 92;
        UIKit.Button(pausePanel, "SETTINGS", () => OpenSettings(true), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(400, 66)); y -= 84;
        UIKit.Button(pausePanel, "RESTART", Restart, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(400, 66)); y -= 84;
        UIKit.Button(pausePanel, "MAIN MENU", MainMenu, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(400, 66));

        // SETTINGS
        settingsPanel = UIKit.BuildSettingsPanel(root, CloseSettings);

        // DEATH
        deathPanel = UIKit.Panel("Death", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 540));
        UIKit.Label(deathPanel, "YOU DIED", 92, UIKit.Blood, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(780, 110), TextAlignmentOptions.Center, true, 20);
        UIKit.Divider(deathPanel, new Vector2(0.5f, 1), new Vector2(0, -168), 640);
        deathStats = UIKit.Label(deathPanel, "", 26, UIKit.Bone, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(760, 90), TextAlignmentOptions.Center, false, 6);
        UIKit.Button(deathPanel, "TRY AGAIN", Restart, new Vector2(0.5f, 0), new Vector2(-170, 80), new Vector2(310, 72), true);
        UIKit.Button(deathPanel, "MAIN MENU", MainMenu, new Vector2(0.5f, 0), new Vector2(170, 80), new Vector2(310, 72));

        // VICTORY
        victoryPanel = UIKit.Panel("Victory", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 540));
        UIKit.Label(victoryPanel, "MISSION COMPLETE", 58, UIKit.Toxic, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(900, 90), TextAlignmentOptions.Center, true, 8);
        UIKit.Label(victoryPanel, "THE HARBOUR IS CLEAR", 24, UIKit.Dim, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(760, 32), TextAlignmentOptions.Center, false, 10);
        UIKit.Divider(victoryPanel, new Vector2(0.5f, 1), new Vector2(0, -198), 640);
        victoryStats = UIKit.Label(victoryPanel, "", 26, UIKit.Bone, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -218), new Vector2(760, 90), TextAlignmentOptions.Center, false, 6);
        UIKit.Button(victoryPanel, "PLAY AGAIN", Restart, new Vector2(0.5f, 0), new Vector2(-170, 80), new Vector2(310, 72), true);
        UIKit.Button(victoryPanel, "MAIN MENU", MainMenu, new Vector2(0.5f, 0), new Vector2(170, 80), new Vector2(310, 72));
    }

    // ------------------------------------------------------------------ state
    private void Update()
    {
        bool dead = player != null && player.IsDead;

        if (GameState.IsGameOver)
        {
            overTimer += Time.unscaledDeltaTime;
            var target = dead ? Screen.Death : Screen.Victory;
            // Give the death fade a moment before the menu appears.
            if (current != target && overTimer > (dead ? 1.6f : 0.4f)) SetScreen(target);
        }
        else
        {
            overTimer = 0f;
            if (GameState.IsPaused)
            {
                if (current != Screen.Pause && current != Screen.Settings) SetScreen(Screen.Pause);
            }
            else if (current != Screen.None) SetScreen(Screen.None);
        }

        group.alpha = Mathf.MoveTowards(group.alpha, current == Screen.None ? 0f : 1f, Time.unscaledDeltaTime * 8f);

        if (current != Screen.None)
        {
            // Menus always need a free cursor (some scripts re-lock it).
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void SetScreen(Screen s)
    {
        current = s;
        bool any = s != Screen.None;
        group.blocksRaycasts = any;
        group.interactable = any;
        dim.gameObject.SetActive(any);
        pausePanel.gameObject.SetActive(s == Screen.Pause);
        settingsPanel.gameObject.SetActive(s == Screen.Settings);
        deathPanel.gameObject.SetActive(s == Screen.Death);
        victoryPanel.gameObject.SetActive(s == Screen.Victory);

        string stats = $"WAVE {Mathf.Max(1, GameState.WaveReached)}     KILLS {GameState.Kills}     HEADSHOTS {GameState.Headshots}\n<size=34><color=#EDE8DB>SCORE {GameState.Score:N0}</color></size>";
        if (s == Screen.Death) deathStats.text = stats;
        if (s == Screen.Victory) victoryStats.text = stats;
        if (any) group.alpha = Mathf.Max(group.alpha, 0.01f);
    }

    // ------------------------------------------------------------------ actions
    private void Resume()
    {
        var pm = FindAnyObjectByType<PauseManager>();
        if (pm != null) pm.ResumeGame(); else GameState.SetPaused(false);
        SetScreen(Screen.None);
    }

    private void OpenSettings(bool fromPause)
    {
        settingsFromPause = fromPause;
        SetScreen(Screen.Settings);
    }

    private void CloseSettings() => SetScreen(settingsFromPause ? Screen.Pause : Screen.None);

    private void Restart()
    {
        GameState.ResetMatch();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void MainMenu()
    {
        GameState.ResetMatch();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (Application.CanStreamedLevelBeLoaded(mainMenuScene)) SceneManager.LoadScene(mainMenuScene);
        else SceneManager.LoadScene(0);
    }
}
