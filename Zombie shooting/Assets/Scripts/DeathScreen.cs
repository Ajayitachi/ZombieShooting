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
    public GameObject waveCountUI;
    public GameObject statusWaveCountUI;

    [Header("Settings")]
    public float duration = 4f;
    public bool showDeadScreen = false;
    [Tooltip("Append wave / kills / score to the death text.")]
    public bool showStats = true;

    private float targetAlpha = 1f;
    private float startAlpha;
    private bool shown;

    private void Start()
    {
        startAlpha = targetImage != null ? targetImage.color.a : 0f;
        if (buttonPanel != null) buttonPanel.SetActive(false);
    }

    private void Update()
    {
        // Kept for backwards compatibility with code that sets the flag directly.
        if (showDeadScreen && !shown) Show();
    }

    public void Show()
    {
        if (shown) return;
        shown = true;
        showDeadScreen = true;

        GameState.EndGame();

        if (waveCountUI != null) waveCountUI.SetActive(false);
        if (statusWaveCountUI != null) statusWaveCountUI.SetActive(false);

        if (showStats && targetText != null)
        {
            string baseText = string.IsNullOrWhiteSpace(targetText.text) ? "YOU DIED" : targetText.text;
            targetText.text = $"{baseText}\n<size={Mathf.RoundToInt(targetText.fontSize * 0.26f)}><color=#B9B3A6>WAVE {Mathf.Max(1, GameState.WaveReached)}    ·    KILLS {GameState.Kills}    ·    HEADSHOTS {GameState.Headshots}    ·    SCORE {GameState.Score}</color></size>";
            targetText.alignment = TextAnchor.LowerCenter; // grow upward so the stats never cover the buttons
            targetText.supportRichText = true;
            targetText.verticalOverflow = VerticalWrapMode.Overflow;   // the stats line was clipped by the text box
            targetText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        StartCoroutine(FadeIn());
        StartCoroutine(FadeOutAllAudio(1.5f));
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetUIAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            elapsed += Time.unscaledDeltaTime; // runs while the game is frozen
            yield return null;
        }
        SetUIAlpha(targetAlpha);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (buttonPanel != null) buttonPanel.SetActive(true);
    }

    private void SetUIAlpha(float alpha)
    {
        if (targetImage != null)
        {
            Color c = targetImage.color; c.a = alpha; targetImage.color = c;
        }
        if (targetText != null)
        {
            Color c = targetText.color; c.a = alpha; targetText.color = c;
        }
    }

    private IEnumerator FadeOutAllAudio(float fadeDuration)
    {
        float startVolume = AudioListener.volume;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        AudioListener.volume = 0f;
        AudioListener.pause = true;
    }

    // --- Button Methods ---
    public void ReplayLevel()
    {
        GameState.ResetMatch();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        GameState.ResetMatch();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
