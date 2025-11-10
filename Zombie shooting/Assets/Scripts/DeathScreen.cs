using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

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
    private bool gamePaused = false;
    private bool isFadingAudio = false;

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
            // 🧠 Pause game time once
            if (!gamePaused)
            {
                Time.timeScale = 0f; // ⏸️ Pause game physics/movement
                gamePaused = true;

                // 🧩 Start smooth audio fade-out
                if (!isFadingAudio)
                    StartCoroutine(FadeOutAllAudio(1.5f)); // fade out over 1.5 seconds
            }

            // Disable wave UI once
            if (waveCountUI != null && waveCountUI.activeSelf)
                waveCountUI.SetActive(false);

            if (statusWaveCountUI != null && statusWaveCountUI.activeSelf)
                statusWaveCountUI.SetActive(false);

            // --- Fade effect (still runs while game is paused) ---
            if (elapsedTime < duration)
            {
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                SetUIAlpha(newAlpha);
                elapsedTime += Time.unscaledDeltaTime; // ⚡ Use unscaled time so fade continues while paused
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
        if (targetImage != null)
        {
            Color imgColor = targetImage.color;
            imgColor.a = alpha;
            targetImage.color = imgColor;
        }

        // Fade text
        if (targetText != null)
        {
            Color txtColor = targetText.color;
            txtColor.a = alpha;
            targetText.color = txtColor;
        }
    }

    // 🧩 Smoothly fade out all audio (using unscaled time)
    IEnumerator FadeOutAllAudio(float duration)
    {
        isFadingAudio = true;

        float startVolume = AudioListener.volume;

        while (AudioListener.volume > 0f)
        {
            AudioListener.volume -= startVolume * (Time.unscaledDeltaTime / duration);
            yield return null;
        }

        AudioListener.volume = 0f;
        AudioListener.pause = true;
        isFadingAudio = false;
    }

    // 🧩 Restore audio when restarting or returning to menu
    private void RestoreAudio()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
    }

    // --- Button Methods ---
    public void ReplayLevel()
    {
        RestoreAudio();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        RestoreAudio();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
