using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject missionCompletePanel;   // Assign in Inspector
    public Text missionText;                  // Optional text for message
    public Button continueButton;             // Optional button

    [Header("Gameplay References")]
    public PlayerController playerController; // Drag your player here

    private bool missionCompleted = false;

    void Start()
    {
        missionCompletePanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);
    }

    public void ShowMissionComplete()
    {
        if (missionCompleted) return;
        missionCompleted = true;

        // Stop player control
        if (playerController != null)
            playerController.enabled = false;

        // Stop time (optional)
        // Time.timeScale = 0f;

        // Show mission complete UI
        missionCompletePanel.SetActive(true);

        if (missionText != null)
            missionText.text = "MISSION COMPLETE";
    }

    private void OnContinue()
    {
        // Optional: load main menu or next level
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
