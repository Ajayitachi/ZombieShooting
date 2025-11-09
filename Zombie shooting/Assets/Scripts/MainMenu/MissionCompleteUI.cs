using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionCompleteUI : MonoBehaviour
{
    [Header("UI Panel")]
    [Tooltip("Assign your Mission Complete Panel here.")]
    public GameObject missionCompletePanel;

    private bool isShowing = false;

    void Start()
    {
        // Make sure it's hidden at the start
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);
    }

    // ✅ Called by ZombieWaveSpawner when mission ends
    public void ShowMissionComplete()
    {
        if (isShowing) return;
        isShowing = true;

        if (missionCompletePanel != null)
        {
            // Instantly show the panel
            missionCompletePanel.SetActive(true);

            // Pause the game
            Time.timeScale = 0f;

            // Unlock and show cursor for button click
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ✅ Continue button → goes back to Main Menu
    public void OnContinueButton()
    {
        // Resume time before loading menu
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Load Main Menu (make sure this scene exists in Build Settings)
        SceneManager.LoadScene("MainMenu");
    }
}
