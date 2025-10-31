using TMPro;
using UnityEngine;
using System.Collections;

public class WaveUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI statusText; // Optional message (e.g., "Wave Completed!")

    private int totalWaves = 0;

    public void InitializeUI(int total)
    {
        totalWaves = total;
        UpdateWaveText(0);
        if (statusText != null)
            statusText.text = "";
    }

    public void UpdateWaveText(int waveIndex)
    {
        int displayWave = waveIndex + 1;
        waveText.text = $"Wave: {displayWave} / {totalWaves}";

        if (statusText != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowTemporaryMessage($"Wave {displayWave} Started!", 2f));
        }
    }

    public void ShowFinalMessage(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        statusText.text = message;
        yield return new WaitForSeconds(duration);
        statusText.text = "";
    }
}
