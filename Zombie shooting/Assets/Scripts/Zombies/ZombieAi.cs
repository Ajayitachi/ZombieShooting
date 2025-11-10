using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CapsuleCollider), typeof(NavMeshAgent), typeof(Animator))]
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

    [Header("Audio Settings")]
    public AudioSource zombieSound;   // 🎵 One sound only

    private Rigidbody rb;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTarget = playerObj.transform;
        }

        if (playerTarget != null)
            playerController = playerTarget.GetComponent<PlayerController>();

        if (playerController == null)
            Debug.LogError($"{gameObject.name} could not find the PlayerController!");

        // 👇 Ensure AudioSource exists
        if (zombieSound == null)
        {
            zombieSound = GetComponent<AudioSource>();
            if (zombieSound == null)
                zombieSound = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (isDead || playerTarget == null) return;

        float speed = agent.velocity.magnitude;
        float normalizedSpeed = speed / agent.speed;
        animator.SetFloat("Speed", normalizedSpeed);

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= attackRange && canAttack)
        {
            agent.isStopped = true;
            StartCoroutine(AttackPlayer());
        }
        else if (distance <= detectionRange && distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);

            // 🎵 Play zombie sound once when chasing
            if (!zombieSound.isPlaying)
                zombieSound.Play();
        }
        else if (distance > detectionRange)
        {
            agent.isStopped = true;
        }
    }

    private IEnumerator AttackPlayer()
    {
        canAttack = false;
        animator.SetTrigger("Attack");
        transform.LookAt(playerTarget.position);
        yield return new WaitForSeconds(0.5f);

        if (playerController != null && !isDead)
        {
            float distance = Vector3.Distance(transform.position, playerTarget.position);
            if (distance <= attackRange)
                playerController.TakeDamage(attackDamage);
        }

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
        animator.SetTrigger("Die");
        agent.isStopped = true;
        agent.enabled = false;

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        yield return new WaitForSeconds(disableDelay);
        gameObject.SetActive(false);
    }
}
