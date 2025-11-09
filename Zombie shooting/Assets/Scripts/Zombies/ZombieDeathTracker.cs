using UnityEngine;

public class ZombieDeathTracker : MonoBehaviour
{
    public ZombieWaveSpawner spawner;
    private ZombieAI zombieAI;

    void Start()
    {
        zombieAI = GetComponent<ZombieAI>();
    }

    // This gets called automatically when the GameObject is disabled
    void OnDisable()
    {
        // If this happens because the zombie died, count it as death
        if (spawner != null && zombieAI != null)
        {
            spawner.OnZombieDeath();
        }
    }
}
