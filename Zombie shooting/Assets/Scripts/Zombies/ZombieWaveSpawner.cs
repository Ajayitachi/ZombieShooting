using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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
    [Tooltip("Seconds before the very first wave starts.")]
    public float firstWaveDelay = 3f;
    [Tooltip("Never keep more than this many zombies alive at once (spawning pauses).")]
    public int maxAliveAtOnce = 12;

    [Header("Difficulty Scaling (per wave after the first)")]
    [Range(0f, 0.5f)] public float speedIncreasePerWave = 0.12f;
    [Range(0f, 0.5f)] public float healthIncreasePerWave = 0.15f;

    [Header("Spawning")]
    [Tooltip("Spawn points closer than this to the player are skipped (no spawning in your face).")]
    public float minSpawnDistance = 10f;

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
    private int zombiesAlive = 0;
    private int zombiesLeftInWave = 0;
    private bool missionCompleteTriggered = false;

    public int ZombiesAlive => zombiesAlive;
    public int ZombiesRemainingInWave => zombiesLeftInWave;
    public int CurrentWave => currentWaveIndex + 1;
    public int TotalWaves => waves != null ? waves.Length : 0;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            else
                Debug.LogWarning("[ZombieWaveSpawner] No player found with tag 'Player'!");
        }

        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("[ZombieWaveSpawner] No waves configured.");
            return;
        }

        if (waveUIManager != null)
            waveUIManager.InitializeUI(waves.Length);

        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        if (waveUIManager != null)
            waveUIManager.ShowStatus("SURVIVE THE HORDE", firstWaveDelay);
        yield return new WaitForSeconds(firstWaveDelay);

        for (currentWaveIndex = 0; currentWaveIndex < waves.Length; currentWaveIndex++)
        {
            Wave wave = waves[currentWaveIndex];
            GameState.WaveReached = currentWaveIndex + 1;
            zombiesLeftInWave = wave.zombieCount;
            Debug.Log($"--- Starting {wave.waveName} ({currentWaveIndex + 1}/{waves.Length}) ---");

            if (waveUIManager != null)
                waveUIManager.UpdateWaveText(currentWaveIndex);

            // Spawn the wave, respecting the alive cap.
            for (int i = 0; i < wave.zombieCount; i++)
            {
                while (zombiesAlive >= maxAliveAtOnce) yield return null;
                SpawnZombie(currentWaveIndex);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // Wait until every zombie of this wave is dead.
            while (zombiesAlive > 0) yield return null;

            bool lastWave = currentWaveIndex >= waves.Length - 1;
            if (lastWave) break;

            Debug.Log($"Wave {wave.waveName} cleared! Next wave in {timeBetweenWaves} seconds...");
            RefillPlayerAmmo();
            if (waveUIManager != null)
                waveUIManager.ShowStatus($"WAVE {currentWaveIndex + 1} CLEARED  ·  AMMO RESTOCKED", timeBetweenWaves);
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        if (!missionCompleteTriggered)
        {
            missionCompleteTriggered = true;
            Debug.Log("[ZombieWaveSpawner] All waves completed! Showing Mission Complete.");
            ShowMissionComplete();
        }
    }

    private void RefillPlayerAmmo()
    {
        if (player == null) return;
        Shoot gun = player.GetComponentInChildren<Shoot>(true);
        if (gun != null) gun.RefillReserve();
    }

    private void SpawnZombie(int waveIndex)
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[ZombieWaveSpawner] Missing zombie prefabs or spawn points!");
            return;
        }

        GameObject zombiePrefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        if (zombiePrefab == null) return;

        Transform spawnPoint = PickSpawnPoint();
        Vector3 pos = spawnPoint.position;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            pos = hit.position;

        GameObject newZombie = Instantiate(zombiePrefab, pos, spawnPoint.rotation);
        zombiesAlive++;

        ZombieAI zombieAI = newZombie.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            zombieAI.SetTarget(player);
            zombieAI.ScaleHealth(1f + healthIncreasePerWave * waveIndex);
        }

        NavMeshAgent agent = newZombie.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.speed *= (1f + speedIncreasePerWave * waveIndex) * Random.Range(0.9f, 1.15f);

        ZombieDeathTracker tracker = newZombie.GetComponent<ZombieDeathTracker>();
        if (tracker == null) tracker = newZombie.AddComponent<ZombieDeathTracker>();
        tracker.spawner = this;
    }

    // Random spawn point that isn't right next to the player (falls back to any point).
    private Transform PickSpawnPoint()
    {
        if (player != null)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Transform t = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (t != null && Vector3.Distance(t.position, player.position) >= minSpawnDistance)
                    return t;
            }
        }
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    public void OnZombieDeath()
    {
        zombiesAlive = Mathf.Max(0, zombiesAlive - 1);
        zombiesLeftInWave = Mathf.Max(0, zombiesLeftInWave - 1);
    }

    private void ShowMissionComplete()
    {
        if (waveCountUI != null) waveCountUI.SetActive(false);
        if (statusWaveCountUI != null) statusWaveCountUI.SetActive(false);

        GameState.EndGame();

        if (missionCompleteScreen != null)
        {
            MissionCompleteUI ui = missionCompleteScreen.GetComponent<MissionCompleteUI>();
            if (ui != null)
                ui.ShowMissionComplete();
            else
                missionCompleteScreen.SetActive(true);
        }
        else
        {
            Debug.LogError("Mission Complete Screen reference is NULL! Please assign it in the Inspector.");
        }
    }
}
