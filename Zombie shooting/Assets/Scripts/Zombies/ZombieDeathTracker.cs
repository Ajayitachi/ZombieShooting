using UnityEngine;

/// <summary>
/// Reports a spawned zombie's removal to the wave spawner exactly once.
/// (Old version counted a death on *any* OnDisable, including scene unload.)
/// </summary>
public class ZombieDeathTracker : MonoBehaviour
{
    public ZombieWaveSpawner spawner;
    private ZombieAI zombieAI;
    private bool reported;

    private void Awake()
    {
        zombieAI = GetComponent<ZombieAI>();
        if (zombieAI != null) zombieAI.Died += HandleDied;
    }

    private void HandleDied(ZombieAI zombie, bool headshot) => Report();

    // Covers zombies destroyed without dying (fell out of the map, etc.),
    // but ignores destruction caused by unloading the scene.
    private void OnDestroy()
    {
        if (zombieAI != null) zombieAI.Died -= HandleDied;
        if (gameObject.scene.isLoaded) Report();
    }

    private void Report()
    {
        if (reported || spawner == null) return;
        reported = true;
        spawner.OnZombieDeath();
    }
}
