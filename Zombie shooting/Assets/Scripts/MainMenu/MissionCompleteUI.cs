using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionCompleteUI : MonoBehaviour
{
    [Header("UI Panel")]
    [Tooltip("Assign your Mission Complete Panel here.")]
    public GameObject missionCompletePanel;

    private bool isShowing = false;

    private void Start()
    {
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);
    }

    // Called by ZombieWaveSpawner when the last wave is cleared
    public void ShowMissionComplete()
    {
        if (isShowing) return;
        isShowing = true;

        GameState.EndGame();
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(true);
    }

    // Continue button → back to Main Menu
    public void OnContinueButton()
    {
        GameState.ResetMatch();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    // Optional "Play Again" button
    public void OnReplayButton()
    {
        GameState.ResetMatch();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
