using System.Collections;
using UnityEngine;
using TMPro;

public class ZombieWaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave";
        public int zombieCount = 5;
        public float spawnInterval = 2f;
    }

    [Header("Wave Settings")]
    public Wave[] waves;
    public float timeBetweenWaves = 5f;

    [Header("References")]
    public GameObject[] zombiePrefabs;
    public Transform[] spawnPoints;
    public Transform player;

    [Header("UI References")]
    public WaveUIManager waveUIManager;
    public GameObject waveCountUI;
    public GameObject statusWaveCountUI;

    [Header("Mission Complete UI")]
    [Tooltip("Assign the GameObject that has the MissionCompleteUI script.")]
    public GameObject missionCompleteScreen;

    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int zombiesAlive = 0;
    private bool missionCompleteTriggered = false;

    void Start()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            else
                Debug.LogWarning("[ZombieWaveSpawner] No player found with tag 'Player'!");
        }

        // Initialize wave UI
        if (waveUIManager != null)
            waveUIManager.InitializeUI(waves.Length);

        // Start first wave
        StartCoroutine(StartNextWave());
    }

    void Update()
    {
        // ? If all waves are complete and no zombies remain
        if (!isSpawning && zombiesAlive == 0 && currentWaveIndex >= waves.Length && !missionCompleteTriggered)
        {
            missionCompleteTriggered = true;
            Debug.Log("[ZombieWaveSpawner] All waves completed! Showing Mission Complete.");
            ShowMissionComplete();
        }

        // ? Continue spawning next wave if available
        if (!isSpawning && zombiesAlive == 0 && currentWaveIndex < waves.Length)
        {
            StartCoroutine(StartNextWave());
        }
    }

    private IEnumerator StartNextWave()
    {
        if (currentWaveIndex >= waves.Length)
            yield break;

        Wave wave = waves[currentWaveIndex];
        isSpawning = true;

        Debug.Log($"--- Starting {wave.waveName} ({currentWaveIndex + 1}/{waves.Length}) ---");

        if (waveUIManager != null)
            waveUIManager.UpdateWaveText(currentWaveIndex);

        for (int i = 0; i < wave.zombieCount; i++)
        {
            SpawnZombie();
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        isSpawning = false;
        currentWaveIndex++;

        // Wait before next wave (if not the last)
        if (currentWaveIndex < waves.Length)
        {
            Debug.Log($"Wave {wave.waveName} completed! Next wave in {timeBetweenWaves} seconds...");
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void SpawnZombie()
    {
        if (zombiePrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[ZombieWaveSpawner] Missing zombie prefabs or spawn points!");
            return;
        }

        GameObject zombiePrefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject newZombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        zombiesAlive++;

        // Assign player to zombie AI if available
        ZombieAI zombieAI = newZombie.GetComponent<ZombieAI>();
        if (zombieAI != null && player != null)
        {
            var playerField = typeof(ZombieAI).GetField("playerTarget",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerField != null)
                playerField.SetValue(zombieAI, player);
        }

        // Track zombie death
        ZombieDeathTracker tracker = newZombie.AddComponent<ZombieDeathTracker>();
        tracker.spawner = this;
    }

    public void OnZombieDeath()
    {
        zombiesAlive--;
        if (zombiesAlive < 0) zombiesAlive = 0;
        Debug.Log($"Zombie died. Remaining: {zombiesAlive}");

        // ? If all waves are done and all zombies are dead
        if (!isSpawning && zombiesAlive == 0 && currentWaveIndex >= waves.Length && !missionCompleteTriggered)
        {
            missionCompleteTriggered = true;
            Debug.Log("[ZombieWaveSpawner] All zombies cleared. Mission complete!");
            ShowMissionComplete();
        }
    }

    private void ShowMissionComplete()
    {
        Debug.Log("[ZombieWaveSpawner] ShowMissionComplete() called!");

        // Disable wave UI
        if (waveCountUI != null)
            waveCountUI.SetActive(false);

        if (statusWaveCountUI != null)
            statusWaveCountUI.SetActive(false);

        // ? Pause the game
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ? Show the Mission Complete UI
        if (missionCompleteScreen != null)
        {
            MissionCompleteUI ui = missionCompleteScreen.GetComponent<MissionCompleteUI>();
            if (ui != null)
                ui.ShowMissionComplete();
            else
                missionCompleteScreen.SetActive(true);

            Debug.Log("Mission Complete Screen activated and game paused!");
        }
        else
        {
            Debug.LogError("Mission Complete Screen reference is NULL! Please assign it in the Inspector.");
        }
    }
}
