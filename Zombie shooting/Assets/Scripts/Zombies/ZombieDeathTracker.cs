using UnityEngine;

public class ZombieDeathTracker : MonoBehaviour
{
  
    public ZombieWaveSpawner spawner;
    private ZombieAI zombieAI;

    void Start()
    {
        zombieAI = GetComponent<ZombieAI>();
    }

    void Update()
    {
        // Check if zombie destroyed or marked dead
        if (zombieAI == null)
        {
            if (spawner != null)
                spawner.OnZombieDeath();

            Destroy(this);
        }
    }
}

