using TMPro;
using UnityEngine;
using System.Collections;

public class WaveUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI statusText; // Temporary messages ("Wave 2 Started!")

    private int totalWaves = 0;
    private Coroutine messageRoutine;

    public void InitializeUI(int total)
    {
        totalWaves = total;
        if (waveText != null) waveText.text = $"WAVE 0 / {totalWaves}";
        if (statusText != null) statusText.text = "";
    }

    public void UpdateWaveText(int waveIndex)
    {
        int displayWave = waveIndex + 1;
        if (waveText != null)
            waveText.text = $"WAVE {displayWave} / {totalWaves}";

        bool last = displayWave == totalWaves;
        ShowStatus(last ? $"FINAL WAVE" : $"WAVE {displayWave}", 2.5f);
    }

    public void ShowStatus(string message, float duration)
    {
        if (statusText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowTemporaryMessage(message, duration));
    }

    public void ShowFinalMessage(string message)
    {
        if (statusText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        statusText.alpha = 1f;
        statusText.text = message;
    }

    private IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        statusText.text = message;
        const float fade = 0.35f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Min(1f, t / fade) * Mathf.Min(1f, (duration - t) / fade);
            statusText.alpha = Mathf.Clamp01(a);
            float scale = 1f + 0.15f * Mathf.Max(0f, 1f - t / fade);
            statusText.rectTransform.localScale = Vector3.one * scale;
            yield return null;
        }
        statusText.text = "";
        statusText.alpha = 1f;
        statusText.rectTransform.localScale = Vector3.one;
    }
}
