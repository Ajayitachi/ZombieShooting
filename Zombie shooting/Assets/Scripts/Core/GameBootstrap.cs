using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds the runtime UI and atmosphere to scenes automatically:
///  • game scenes (have a PlayerController): atmosphere, HUD, pause/death/victory menus
///  • menu scenes (named "...Menu..." or using the old menu scripts): the new main menu
/// so nothing has to be hand-wired into the scenes.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Setup();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Setup();

    private static void Setup()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(UIKit.PrefVolume, 1f);

        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            if (IsMenuScene() && Object.FindAnyObjectByType<MainMenuUI>() == null)
                new GameObject("Main Menu UI").AddComponent<MainMenuUI>();
            return;
        }

        // Every player needs a look controller; some scene versions lost theirs (no up/down look).
        if (player.GetComponentInChildren<MouseLook>(true) == null)
        {
            var look = player.gameObject.AddComponent<MouseLook>();
            look.Configure(player.transform, false);
        }

        if (Object.FindAnyObjectByType<AtmosphereDirector>() != null) return;

        var go = new GameObject("Game Director");
        go.AddComponent<AtmosphereDirector>();
        go.AddComponent<GameHUD>();
        go.AddComponent<GameMenusUI>();
    }

    private static bool IsMenuScene()
    {
        string n = SceneManager.GetActiveScene().name.ToLowerInvariant();
        return n.Contains("menu")
            || Object.FindAnyObjectByType<SettingsManager>(FindObjectsInactive.Include) != null;
    }
}
