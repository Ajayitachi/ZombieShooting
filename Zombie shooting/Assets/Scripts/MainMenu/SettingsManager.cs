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
    public string defaultGameScene = "GameScene"; // fallback if no map selected

    private string selectedMap;

    void Start()
    {
        // Play button → loads the selected map or default
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);

        // Map buttons → load map directly
        if (map1Button != null)
            map1Button.onClick.AddListener(() => LoadMap("Map1"));

        if (map2Button != null)
            map2Button.onClick.AddListener(() => LoadMap("Map2"));
    }

    // Called when "Play" button is clicked
    private void PlayGame()
    {
        string sceneToLoad = string.IsNullOrEmpty(selectedMap) ? defaultGameScene : selectedMap;
        Debug.Log("SettingsManager: Loading Scene -> " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad);
    }

    // Called when a map is selected
    private void LoadMap(string mapName)
    {
        selectedMap = mapName;
        Debug.Log("SettingsManager: Map Selected -> " + selectedMap);
        SceneManager.LoadScene(selectedMap);
    }
}
