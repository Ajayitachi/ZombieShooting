using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathScreen : MonoBehaviour
{
    [Header("UI References")]
    public Image targetImage;
    public Text targetText;
    public GameObject buttonPanel; // Parent object that contains Replay/MainMenu buttons

    [Header("Settings")]
    public float duration = 4f;
    public bool showDeadScreen = false;

    private float targetAlpha = 1f;
    private float startAlpha;
    private float elapsedTime = 0f;
    private bool fadeComplete = false;

    void Start()
    {
        startAlpha = targetImage.color.a;

        // Ensure buttons are hidden at start
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
    }

    void Update()
    {
        if (showDeadScreen)
        {
            // --- This part is the same ---
            // Fade effect
            if (elapsedTime < duration)
            {
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                SetUIAlpha(newAlpha);
                elapsedTime += Time.deltaTime;
            }
            // --- This is the MODIFIED block ---
            else if (!fadeComplete)
            {
                fadeComplete = true;

                // Time.timeScale = 0f; // <-- MODIFICATION: Removed this line. It was freezing your buttons.

                // MODIFICATION: Added these two lines to unlock and show your mouse
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (buttonPanel != null)
                    buttonPanel.SetActive(true); // Show replay/main menu buttons
            }
        }
    }

    void SetUIAlpha(float alpha)
    {
        // Fade image
        Color imgColor = targetImage.color;
        imgColor.a = alpha;
        targetImage.color = imgColor;

        // Fade text
        Color txtColor = targetText.color;
        txtColor.a = alpha;
        targetText.color = txtColor;
    }

    // --- Button Methods ---
    public void ReplayLevel()
    {
        Time.timeScale = 1f; // Resume time (good to keep this here just in case)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Resume time
        Cursor.lockState = CursorLockMode.None; // Ensure cursor is still free for main menu
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu"); // Make sure this scene is added to Build Settings
    }
}