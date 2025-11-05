using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Player Health & Damage")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public Slider healthSlider;              // Health bar UI
    public GameObject damageFlash;           // Red flash overlay
    public DeathScreen deathScreen;          // Death screen UI (assign in Inspector)

    [Header("References")]
    public CharacterController controller;   // CharacterController component
    public Camera playerCamera;              // Player camera reference

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float jumpHeight = 2f;

    private float verticalVelocity;

    [Header("Camera Bob")]
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmplitude = 0.05f;
    private Vector3 originalCameraPos;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.1f;

    [Header("Movement Input")]
    private float moveInput;
    private float strafeInput;

    [Header("Footstep Settings")]
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    [SerializeField] private float baseStepSpeed = 0.5f;
    private float footstepTimer;

    private CanvasGroup flashCanvasGroup;

    private void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        if (playerCamera != null)
            originalCameraPos = playerCamera.transform.localPosition;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Damage flash setup
        if (damageFlash != null)
        {
            damageFlash.SetActive(false);
            flashCanvasGroup = damageFlash.GetComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        HandleInput();
        MovePlayer();
        HandleFootsteps();

        // Smooth health bar update
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, currentHealth, Time.deltaTime * 10f);
        }
    }

    // ------------------------------
    // 🔹 INPUT HANDLER
    // ------------------------------
    private void HandleInput()
    {
        moveInput = Input.GetAxis("Vertical");
        strafeInput = Input.GetAxis("Horizontal");
    }

    // ------------------------------
    // 🔹 MOVEMENT HANDLER
    // ------------------------------
    private void MovePlayer()
    {
        Vector3 move = new Vector3(strafeInput, 0, moveInput);
        if (move.magnitude > 1f) move.Normalize();

        // Convert to world space and move
        move = transform.TransformDirection(move) * moveSpeed;
        move.y = CalculateVerticalForce();
        controller.Move(move * Time.deltaTime);

        CameraBob(move.magnitude);
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
    // 🔹 CAMERA BOB
    // ------------------------------
    private void CameraBob(float speed)
    {
        if (playerCamera == null) return;

        if (speed > 0.1f)
        {
            float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            playerCamera.transform.localPosition = originalCameraPos + new Vector3(0, bobOffset, 0);
        }
        else
        {
            playerCamera.transform.localPosition = originalCameraPos;
        }
    }

    // ------------------------------
    // 🔹 FOOTSTEPS
    // ------------------------------
    private void HandleFootsteps()
    {
        if (!controller.isGrounded) return;
        if (moveInput == 0 && strafeInput == 0) return;
        if (footstepSource == null || footstepClips.Length == 0) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            footstepSource.PlayOneShot(clip);
            footstepTimer = baseStepSpeed;
        }
    }

    // ------------------------------
    // 🔹 CAMERA SHAKE
    // ------------------------------
    public void ShakeCamera()
    {
        if (playerCamera != null) StartCoroutine(DoCameraShake());
    }

    private IEnumerator DoCameraShake()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            playerCamera.transform.localPosition = originalCameraPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        playerCamera.transform.localPosition = originalCameraPos;
    }

    // ------------------------------
    // 🔹 DAMAGE HANDLER
    // ------------------------------
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"💥 Player took {damageAmount} damage! Health = {currentHealth}");

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        StartCoroutine(DamageFlashEffect());
        ShakeCamera();

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator DamageFlashEffect()
    {
        if (damageFlash == null) yield break;

        damageFlash.SetActive(true);

        if (flashCanvasGroup != null)
        {
            flashCanvasGroup.alpha = 1f;
            float fadeTime = 0.3f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                flashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }
            flashCanvasGroup.alpha = 0f;
            damageFlash.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0.15f);
            damageFlash.SetActive(false);
        }
    }

    // ------------------------------
    // 🔹 DEATH HANDLER
    // ------------------------------
    void Die()
    {
        if (deathScreen != null)
            deathScreen.showDeadScreen = true;

        Debug.Log("💀 Player is dead!");
    }
}
