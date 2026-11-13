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
    private CapsuleCollider capsule;

    [Header("AI Settings")]
    public float detectionRange = 20f;
    public float attackRange = 2f;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;
    [Tooltip("Seconds into the attack animation when the hit lands.")]
    public float attackHitDelay = 0.5f;
    [Tooltip("Turn speed (degrees/sec) used to face the player while attacking.")]
    public float turnSpeed = 540f;
    private bool canAttack = true;
    private bool isAggro;

    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    [Header("Death Settings")]
    public float disableDelay = 3f;

    [Header("Audio Settings")]
    public AudioSource zombieSound;   // groan loop / one-shot while chasing

    private Rigidbody rb;

    // Animator parameter hashes (only used if the controller actually has them)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private bool hasSpeed, hasAttack, hasDie, hasHit;

    /// <summary>Raised once when this zombie dies. Bool = killed by a headshot.</summary>
    public event System.Action<ZombieAI, bool> Died;

    public bool IsDead => isDead;
    public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    public CapsuleCollider Capsule => capsule;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;

        foreach (var p in animator.parameters)
        {
            if (p.nameHash == SpeedHash) hasSpeed = true;
            else if (p.nameHash == AttackHash) hasAttack = true;
            else if (p.nameHash == DieHash) hasDie = true;
            else if (p.nameHash == HitHash) hasHit = true;
        }

        // Agent rotates itself while walking; we rotate manually while attacking.
        agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, attackRange * 0.8f);
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTarget = playerObj.transform;
        }
        SetTarget(playerTarget);

        if (zombieSound == null)
        {
            zombieSound = GetComponent<AudioSource>();
            if (zombieSound == null)
                zombieSound = gameObject.AddComponent<AudioSource>();
        }
        zombieSound.spatialBlend = 1f;               // 3D sound so you can hear where they come from
        zombieSound.rolloffMode = AudioRolloffMode.Linear;
        zombieSound.maxDistance = Mathf.Max(zombieSound.maxDistance, 30f);
        zombieSound.pitch = Random.Range(0.85f, 1.15f); // variety between zombies

        // Snap onto the NavMesh if we spawned slightly off it.
        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            agent.Warp(hit.position);
    }

    /// <summary>Assigns the target to chase (used by the wave spawner).</summary>
    public void SetTarget(Transform target)
    {
        playerTarget = target;
        playerController = target != null ? target.GetComponentInParent<PlayerController>() : null;
        if (target != null && playerController == null)
            Debug.LogWarning($"{name}: target has no PlayerController, zombie can't deal damage.");
    }

    /// <summary>Multiplies max (and current) health — used for wave difficulty scaling.</summary>
    public void ScaleHealth(float multiplier)
    {
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * multiplier));
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead || playerTarget == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (hasSpeed)
        {
            float normalizedSpeed = agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f;
            animator.SetFloat(SpeedHash, normalizedSpeed, 0.1f, Time.deltaTime);
        }

        if (playerController != null && playerController.IsDead)
        {
            agent.isStopped = true;
            return;
        }

        float distance = FlatDistance(transform.position, playerTarget.position);

        if (distance <= attackRange)
        {
            agent.isStopped = true;
            FacePlayer();
            if (canAttack) StartCoroutine(AttackPlayer());
        }
        else if (isAggro || distance <= detectionRange)
        {
            isAggro = true; // once a zombie has seen you it keeps hunting
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);

            if (!zombieSound.isPlaying && zombieSound.clip != null)
                zombieSound.Play();
        }
        else
        {
            agent.isStopped = true;
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private IEnumerator AttackPlayer()
    {
        canAttack = false;
        if (hasAttack) animator.SetTrigger(AttackHash);

        yield return new WaitForSeconds(attackHitDelay);

        if (!isDead && playerController != null && !playerController.IsDead &&
            FlatDistance(transform.position, playerTarget.position) <= attackRange + 0.5f)
        {
            playerController.TakeDamage(attackDamage, transform.position);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackCooldown - attackHitDelay));
        canAttack = true;
    }

    public void TakeDamage(int damageAmount) => TakeDamage(damageAmount, false);

    public void TakeDamage(int damageAmount, bool headshot)
    {
        if (isDead) return;
        currentHealth -= damageAmount;
        isAggro = true; // getting shot always makes the zombie chase you

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            GameState.RegisterHit(true);
            StartCoroutine(Die(headshot));
        }
        else
        {
            GameState.RegisterHit(false);
            if (hasHit) animator.SetTrigger(HitHash);
            StartCoroutine(Stagger());
        }
    }

    // Brief slow-down when shot so hits feel like they land.
    private IEnumerator Stagger()
    {
        if (!agent.enabled) yield break;
        float original = agent.speed;
        agent.speed = original * 0.35f;
        yield return new WaitForSeconds(0.25f);
        if (!isDead && agent.enabled) agent.speed = original;
    }

    private IEnumerator Die(bool headshot)
    {
        isDead = true; // AttackPlayer checks this flag, so no damage lands after death
        if (hasDie) animator.SetTrigger(DieHash);
        if (zombieSound != null) zombieSound.Stop();

        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        agent.enabled = false;

        if (capsule) capsule.enabled = false;

        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        GameState.RegisterKill(headshot);
        Died?.Invoke(this, headshot);

        yield return new WaitForSeconds(disableDelay);
        Destroy(gameObject);
    }
}
