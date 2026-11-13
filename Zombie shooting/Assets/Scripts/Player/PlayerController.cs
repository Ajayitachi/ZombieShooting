using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Player Health & Damage")]
    public int maxHealth = 100;
    public int currentHealth;
    [Tooltip("Seconds without taking damage before health starts regenerating (0 = off).")]
    [SerializeField] private float regenDelay = 5f;
    [SerializeField] private float regenPerSecond = 8f;

    [Header("UI")]
    public Slider healthSlider;              // Health bar UI
    public GameObject damageFlash;           // Red flash overlay
    public DeathScreen deathScreen;          // Death screen UI (assign in Inspector)

    [Header("References")]
    public CharacterController controller;   // CharacterController component
    public Camera playerCamera;              // Player camera reference

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float jumpHeight = 2f;

    private float verticalVelocity;
    private Vector3 horizontalVelocity;

    [Header("Camera Bob")]
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmplitude = 0.05f;
    private Vector3 originalCameraPos;
    private float bobTimer;
    private Vector3 bobOffset;
    private Vector3 shakeOffset;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.1f;

    [Header("Movement Input")]
    private float moveInput;
    private float strafeInput;
    private bool sprinting;

    [Header("Footstep Settings")]
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    [SerializeField] private float baseStepSpeed = 0.5f;
    private float footstepTimer;

    [Header("Damage Sound Settings")]
    public AudioSource damageAudioSource;   // Source for damage sounds
    public AudioClip damageSound;           // Hurt/grunt sound

    private CanvasGroup flashCanvasGroup;
    private Coroutine flashRoutine;
    private float lastDamageTime = -999f;
    private float regenBuffer;

    public bool IsDead { get; private set; }
    public bool IsSprinting => sprinting;
    public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    /// <summary>0 when still, 1 when walking, ~1.6 when sprinting (used for crosshair spread).</summary>
    public float MoveAmount { get; private set; }

    /// <summary>Raised when hurt. Vector3 = world position of the attacker.</summary>
    public event System.Action<int, Vector3> Damaged;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        cameraRig = ResolveCameraRig();
        if (cameraRig != null)
            originalCameraPos = cameraRig.localPosition;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (damageFlash != null)
        {
            damageFlash.SetActive(false);
            flashCanvasGroup = damageFlash.GetComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        if (IsDead || !GameState.CanPlay) return;

        HandleInput();
        MovePlayer();
        HandleFootsteps();
        Regenerate();
        ApplyCameraOffset();

        if (healthSlider != null)
            healthSlider.value = Mathf.Lerp(healthSlider.value, currentHealth, Time.deltaTime * 10f);
    }

    // ------------------------------
    // INPUT
    // ------------------------------
    private void HandleInput()
    {
        moveInput = Input.GetAxisRaw("Vertical");
        strafeInput = Input.GetAxisRaw("Horizontal");
        // Sprint only while moving forward
        sprinting = Input.GetKey(KeyCode.LeftShift) && moveInput > 0.1f;
    }

    // ------------------------------
    // MOVEMENT
    // ------------------------------
    private void MovePlayer()
    {
        Vector3 input = new Vector3(strafeInput, 0, moveInput);
        if (input.sqrMagnitude > 1f) input.Normalize();

        float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);
        Vector3 target = transform.TransformDirection(input) * speed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * speed * Time.deltaTime);

        Vector3 move = horizontalVelocity;
        move.y = CalculateVerticalForce();
        controller.Move(move * Time.deltaTime);

        MoveAmount = horizontalVelocity.magnitude / Mathf.Max(0.01f, moveSpeed);
        CameraBob();
    }

    private float CalculateVerticalForce()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // keep grounded
            if (Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        return verticalVelocity;
    }

    // ------------------------------
    // CAMERA BOB + SHAKE (combined so they don't overwrite each other)
    // ------------------------------
    private void CameraBob()
    {
        if (controller.isGrounded && MoveAmount > 0.1f)
        {
            bobTimer += Time.deltaTime * bobFrequency * Mathf.PI * Mathf.Max(1f, MoveAmount);
            float amp = bobAmplitude * Mathf.Clamp(MoveAmount, 0f, 1.6f);
            bobOffset = new Vector3(Mathf.Cos(bobTimer * 0.5f) * amp * 0.5f, Mathf.Sin(bobTimer) * amp, 0f);
        }
        else
        {
            bobOffset = Vector3.Lerp(bobOffset, Vector3.zero, Time.deltaTime * 8f);
        }
    }

    private void ApplyCameraOffset()
    {
        if (cameraRig != null)
            cameraRig.localPosition = originalCameraPos + bobOffset + shakeOffset;
    }

    // When a Cinemachine camera drives the view, moving the Camera itself does nothing
    // (Cinemachine overwrites it), so bob/shake are applied to the follow target instead.
    private Transform cameraRig;
    private Transform ResolveCameraRig()
    {
        if (playerCamera == null) return null;
        bool cinemachine = false;
        foreach (var c in playerCamera.GetComponents<Component>())
            if (c != null && c.GetType().Name == "CinemachineBrain") cinemachine = true;
        if (cinemachine)
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name.ToLowerInvariant().StartsWith("follow")) return t;
        return playerCamera.transform;
    }

    // ------------------------------
    // FOOTSTEPS
    // ------------------------------
    private void HandleFootsteps()
    {
        if (!controller.isGrounded) return;
        if (MoveAmount < 0.2f) return;
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            footstepSource.pitch = Random.Range(0.9f, 1.1f);
            footstepSource.PlayOneShot(clip);
            footstepTimer = baseStepSpeed / (sprinting ? sprintMultiplier : 1f);
        }
    }

    // ------------------------------
    // HEALTH REGEN
    // ------------------------------
    private void Regenerate()
    {
        if (regenDelay <= 0f || currentHealth >= maxHealth) return;
        if (Time.time - lastDamageTime < regenDelay) return;

        regenBuffer += regenPerSecond * Time.deltaTime;
        if (regenBuffer >= 1f)
        {
            int add = Mathf.FloorToInt(regenBuffer);
            regenBuffer -= add;
            currentHealth = Mathf.Min(maxHealth, currentHealth + add);
        }
    }

    // ------------------------------
    // CAMERA SHAKE
    // ------------------------------
    public void ShakeCamera() => ShakeCamera(shakeMagnitude, shakeDuration);

    public void ShakeCamera(float magnitude, float duration)
    {
        if (playerCamera != null && isActiveAndEnabled) StartCoroutine(DoCameraShake(magnitude, duration));
    }

    private IEnumerator DoCameraShake(float magnitude, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float damper = 1f - elapsed / duration;
            shakeOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * magnitude * damper;
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }

    // ------------------------------
    // DAMAGE
    // ------------------------------
    public void TakeDamage(int damageAmount) => TakeDamage(damageAmount, transform.position + transform.forward);

    public void TakeDamage(int damageAmount, Vector3 sourcePosition)
    {
        if (IsDead || GameState.IsGameOver) return;

        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0, maxHealth);
        lastDamageTime = Time.time;
        regenBuffer = 0f;

        if (damageAudioSource != null && damageSound != null)
            damageAudioSource.PlayOneShot(damageSound);

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DamageFlashEffect());
        ShakeCamera();
        Damaged?.Invoke(damageAmount, sourcePosition);

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator DamageFlashEffect()
    {
        if (damageFlash == null) yield break;

        damageFlash.SetActive(true);

        if (flashCanvasGroup != null)
        {
            const float fadeTime = 0.35f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                flashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }
            flashCanvasGroup.alpha = 0f;
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.15f);
        }
        damageFlash.SetActive(false);
    }

    // ------------------------------
    // DEATH
    // ------------------------------
    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        shakeOffset = Vector3.zero;
        Debug.Log("Player is dead!");

        if (deathScreen != null)
            deathScreen.Show();
        else
            GameState.EndGame();
    }
}
