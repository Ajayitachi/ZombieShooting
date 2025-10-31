using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// --- ANIMATION --- Added Animator to the RequireComponent list
[RequireComponent(typeof(CapsuleCollider), typeof(NavMeshAgent), typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    [Header("AI References")]
    [SerializeField] private Transform playerTarget;
    private PlayerController playerController;
    private NavMeshAgent agent;
    private Animator animator; // Already defined, which is perfect

    [Header("AI Settings")]
    public float detectionRange = 20f;
    public float attackRange = 2f;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    private bool canAttack = true;

    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    [Header("Death Settings")]
    public float disableDelay = 3f;

    private Rigidbody rb;

    private void Start()
    {
        // Cache components
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Already here, great!
        rb = GetComponent<Rigidbody>();

        // Set health
        currentHealth = maxHealth;

        // --- MODIFIED SECTION START ---
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }
        if (playerTarget != null)
        {
            playerController = playerTarget.GetComponent<PlayerController>();
        }
        if (playerController == null)
        {
            Debug.LogError($"{gameObject.name} could not find the PlayerController!");
        }
        // --- MODIFIED SECTION END ---
    }

    private void Update()
    {
        if (isDead) return;
        if (playerTarget == null) return;

        // --- ANIMATION ---
        // This block reads the agent's current speed and sends it to the "Speed"
        // parameter in your Blend Tree. This handles both walking and idling.
        float speed = agent.velocity.magnitude;
        float normalizedSpeed = speed / agent.speed; // Converts to a 0-1 value
        animator.SetFloat("Speed", normalizedSpeed);
        // --- END ANIMATION ---

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // --- I re-ordered these checks for cleaner logic ---

        // Attack player
        if (distance <= attackRange && canAttack)
        {
            agent.isStopped = true;
            StartCoroutine(AttackPlayer());
        }
        // Chase player
        else if (distance <= detectionRange && distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
        }
        // Player is out of range, stop. (Your 'else' was empty, I filled it)
        else if (distance > detectionRange)
        {
            agent.isStopped = true;
            // The animator.SetFloat("Speed") above will handle returning to Idle
        }
    }

    private IEnumerator AttackPlayer()
    {
        canAttack = false;

        // --- ANIMATION ---
        // Trigger the attack animation
        animator.SetTrigger("Attack");
        // --- END ANIMATION ---

        // Make the zombie look at the player when attacking
        transform.LookAt(playerTarget.position);

        yield return new WaitForSeconds(0.5f); // small attack delay for realism

        if (playerController != null && !isDead)
        {
            // Re-check distance in case player moved out of range during the 0.5s wind-up
            float distance = Vector3.Distance(transform.position, playerTarget.position);
            if (distance <= attackRange)
            {
                playerController.TakeDamage(attackDamage);
                Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage");
            }
        }

        // Wait for the full cooldown (minus the wind-up time)
        yield return new WaitForSeconds(attackCooldown - 0.5f);
        canAttack = true;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        isDead = true;

        // --- ANIMATION ---
        // Trigger the death animation
        animator.SetTrigger("Die");
        // --- END ANIMATION ---

        agent.isStopped = true;
        agent.enabled = false; // Disable NavMeshAgent completely

        // Disable collider & stop movement
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        yield return new WaitForSeconds(disableDelay);

        // Disable zombie object
        gameObject.SetActive(false);
    }
}