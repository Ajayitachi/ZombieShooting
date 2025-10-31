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
    [Tooltip("Zombie prefabs to spawn.")]
    public GameObject[] zombiePrefabs;

    [Tooltip("Spawn points in the scene.")]
    public Transform[] spawnPoints;

    [Tooltip("Player reference (auto-assigned if empty).")]
    public Transform player;

    [Header("UI References")]
    [Tooltip("Assign the WaveUIManager here.")]
    public WaveUIManager waveUIManager;

    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int zombiesAlive = 0;

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

        // Start the first wave
        StartCoroutine(StartNextWave());
    }

    void Update()
    {
        // If all zombies are dead and we finished spawning current wave, move to next
        if (!isSpawning && zombiesAlive == 0 && currentWaveIndex < waves.Length)
        {
            StartCoroutine(StartNextWave());
        }
    }

    private IEnumerator StartNextWave()
    {
        if (currentWaveIndex >= waves.Length)
        {
            Debug.Log("[ZombieWaveSpawner] All waves completed!");
            if (waveUIManager != null)
                waveUIManager.ShowFinalMessage("All Waves Completed!");
            yield break;
        }

        Wave wave = waves[currentWaveIndex];
        isSpawning = true;

        Debug.Log($"--- Starting {wave.waveName} ---");

        // Update UI for new wave
        if (waveUIManager != null)
            waveUIManager.UpdateWaveText(currentWaveIndex);

        for (int i = 0; i < wave.zombieCount; i++)
        {
            SpawnZombie();
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        isSpawning = false;
        currentWaveIndex++;

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

        // Link player reference to the zombie
        ZombieAI zombieAI = newZombie.GetComponent<ZombieAI>();
        if (zombieAI != null && player != null)
        {
            var playerField = typeof(ZombieAI).GetField("playerTarget",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerField != null)
                playerField.SetValue(zombieAI, player);
        }

        // Track when zombie dies
        ZombieDeathTracker tracker = newZombie.AddComponent<ZombieDeathTracker>();
        tracker.spawner = this;
    }

    public void OnZombieDeath()
    {
        zombiesAlive--;
        if (zombiesAlive < 0) zombiesAlive = 0;
    }
}
