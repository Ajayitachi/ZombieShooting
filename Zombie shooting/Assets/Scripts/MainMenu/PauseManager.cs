using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenu;        // Assign the PauseMenu panel here
    public Button resumeButton;
    public Button quitButton;

    private void Start()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        // Esc must not toggle anything once the death / mission-complete screen is up
        // (it used to un-freeze the game behind the death screen).
        if (GameState.IsGameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (GameState.IsPaused) ResumeGame();
            else PauseGame();
        }
    }

    // Lose focus (alt-tab) → pause automatically.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && !GameState.IsGameOver && !GameState.IsPaused && !Application.isEditor)
            PauseGame();
    }

    public void PauseGame()
    {
        if (GameState.IsGameOver) return;
        if (pauseMenu != null) pauseMenu.SetActive(true);
        GameState.SetPaused(true);
    }

    public void ResumeGame()
    {
        if (GameState.IsGameOver) return;
        if (pauseMenu != null) pauseMenu.SetActive(false);
        GameState.SetPaused(false);
    }

    public void QuitGame()
    {
        GameState.ResetMatch();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
