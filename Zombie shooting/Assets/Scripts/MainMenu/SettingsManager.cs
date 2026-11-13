using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button map1Button;
    public Button map2Button; // optional, add more maps if needed

    [Header("Scene Names")]
    public string defaultGameScene = "Map1"; // fallback if no map selected

    private string selectedMap;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (map1Button != null) map1Button.onClick.AddListener(() => LoadMap("Map1"));
        if (map2Button != null) map2Button.onClick.AddListener(() => LoadMap("Map2"));
    }

    private void PlayGame()
    {
        string sceneToLoad = string.IsNullOrEmpty(selectedMap) ? defaultGameScene : selectedMap;
        LoadSafe(sceneToLoad);
    }

    private void LoadMap(string mapName)
    {
        selectedMap = mapName;
        LoadSafe(selectedMap);
    }

    // Only load scenes that are actually in Build Settings; otherwise fall back to Map1.
    private static void LoadSafe(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"SettingsManager: scene '{sceneName}' is not in Build Settings, loading Map1 instead.");
            sceneName = "Map1";
        }
        SceneManager.LoadScene(sceneName);
    }
}
