using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathScreen : MonoBehaviour
{
    [Header("UI References")]
    public Image targetImage;
    public Text targetText;
    public GameObject buttonPanel; // Parent object that contains Replay/MainMenu buttons

    [Header("External References")]
    public GameObject waveCountUI;        // 👈 Assign "Wave Count" GameObject
    public GameObject statusWaveCountUI;  // 👈 Assign "Status Wave count" GameObject

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
            // Disable wave UI only once when death starts
            if (waveCountUI != null && waveCountUI.activeSelf)
                waveCountUI.SetActive(false);

            if (statusWaveCountUI != null && statusWaveCountUI.activeSelf)
                statusWaveCountUI.SetActive(false);

            // --- Fade effect ---
            if (elapsedTime < duration)
            {
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                SetUIAlpha(newAlpha);
                elapsedTime += Time.deltaTime;
            }
            else if (!fadeComplete)
            {
                fadeComplete = true;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (buttonPanel != null)
                    buttonPanel.SetActive(true);
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
