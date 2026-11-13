using UnityEngine;

/// <summary>
/// Global match state shared by gameplay scripts (pause, game over, score).
/// Reset automatically every time a scene loads.
/// </summary>
public static class GameState
{
    public static bool IsPaused { get; private set; }
    public static bool IsGameOver { get; private set; }

    public static int Kills { get; private set; }
    public static int Headshots { get; private set; }
    public static int Score { get; private set; }
    public static int WaveReached { get; set; }

    /// <summary>True while the player is allowed to move, look and shoot.</summary>
    public static bool CanPlay => !IsPaused && !IsGameOver && Time.timeScale > 0f;

    public static event System.Action<int, bool> OnKill;      // (points, headshot)
    public static event System.Action<bool> OnHit;             // (killingBlow)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        OnKill = null;
        OnHit = null;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        ResetMatch();
    }

    private static void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (mode == UnityEngine.SceneManagement.LoadSceneMode.Single) ResetMatch();
    }

    public static void ResetMatch()
    {
        IsPaused = false;
        IsGameOver = false;
        Kills = 0;
        Headshots = 0;
        Score = 0;
        WaveReached = 0;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f); // menu setting
    }

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused || IsGameOver ? 0f : 1f;
        AudioListener.pause = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    public static void EndGame()
    {
        IsGameOver = true;
        IsPaused = false;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void RegisterHit(bool killingBlow) => OnHit?.Invoke(killingBlow);

    public static void RegisterKill(bool headshot)
    {
        Kills++;
        if (headshot) Headshots++;
        int points = headshot ? 150 : 100;
        Score += points;
        OnKill?.Invoke(points, headshot);
    }
}
