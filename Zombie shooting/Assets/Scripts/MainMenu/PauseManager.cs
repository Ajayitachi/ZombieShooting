using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenu;        // Assign the PauseMenu panel here
    public Button resumeButton;
    public Button quitButton;

    private bool isPaused = false;

    void Start()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        // Press Escape to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        Time.timeScale = 0f;           // Freeze game time
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPaused = true;
    }

    public void ResumeGame()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        Time.timeScale = 1f;           // Resume time
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isPaused = false;
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game pressed!");

        // 🔹 1. RESET TIME
        // You MUST do this, or the Main Menu will be frozen
        Time.timeScale = 1f;

        // 🔹 2. ENSURE CURSOR IS VISIBLE FOR THE MENU
        // It's good practice to set this explicitly for the menu scene
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 🔹 3. NOW LOAD THE SCENE
        SceneManager.LoadScene("MainMenu");
    }
}
