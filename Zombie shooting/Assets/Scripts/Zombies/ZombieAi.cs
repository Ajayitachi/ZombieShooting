using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CapsuleCollider), typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    [Header("AI References")]
    [SerializeField] private Transform playerTarget;
    private PlayerController playerController;
    private NavMeshAgent agent;
    private Animator animator;

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
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Set health
        currentHealth = maxHealth;

        // Auto find player
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }
    }

    private void Update()
    {
        if (isDead) return;
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // Chase player
        if (distance <= detectionRange && distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            if (animator) animator.SetBool("isWalking", true);
        }
        // Attack player
        else if (distance <= attackRange && canAttack)
        {
            agent.isStopped = true;
            if (animator) animator.SetBool("isWalking", false);
            StartCoroutine(AttackPlayer());
        }
        else
        {
            if (animator) animator.SetBool("isWalking", false);
        }
    }

    private IEnumerator AttackPlayer()
    {
        canAttack = false;

        // Attack animation
        if (animator) animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.5f); // small attack delay for realism

        if (playerController != null && !isDead)
        {
            playerController.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage");
        }

        yield return new WaitForSeconds(attackCooldown);
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
        if (animator) animator.SetTrigger("Die");

        agent.isStopped = true;

        // Disable collider & stop movement
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col) col.enabled = false;

        // Optional: disable rigidbody physics
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
