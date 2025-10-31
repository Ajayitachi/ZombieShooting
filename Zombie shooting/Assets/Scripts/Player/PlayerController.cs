using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Player Health & Damage")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI")]
    public Slider healthSlider;              // 🔹 Health bar
    public GameObject damageFlash;           // 🔹 Red flash prefab (GameObject in Canvas)
    public DeathScreen deathScreen;          // 🔹 Death screen (assign in inspector)

    [Header("References")]
    public CharacterController controller;
    [SerializeField] private Transform cameraTarget;
    public Camera playerCamera;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float rotSpeed = 50f;

    private float verticalVelocity;
    private float xRotation;

    [Header("Camera Bob")]
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmplitude = 0.05f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    private Vector3 originalCameraPos;

    [Header("Input")]
    [SerializeField] private float mouseSensitivity = 100f;
    private float moveInput;
    private float turnInput;
    private float mouseX;
    private float mouseY;

    [Header("Recoil")]
    private Vector3 targetRecoil = Vector3.zero;
    private Vector3 currentRecoil = Vector3.zero;

    [Header("Footstep Settings")]
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;
    [SerializeField] private float baseStepSpeed = 0.5f;
    private float footstepTimer;

    // 🔹 New for red flash fading
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

        // 🔹 Setup flash prefab
        if (damageFlash != null)
        {
            damageFlash.SetActive(false);
            flashCanvasGroup = damageFlash.GetComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        InputManagement();
        Movement();
        Turn();
        HandleFootsteps();

        // Smooth health bar transition
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, currentHealth, Time.deltaTime * 10f);
        }
    }

    private void Movement()
    {
        Vector3 move = new Vector3(turnInput, 0, moveInput);
        if (move.magnitude > 1f) move.Normalize();
        move = transform.TransformDirection(move) * moveSpeed;

        move.y = VerticalForceCalculation();
        controller.Move(move * Time.deltaTime);

        CameraBob(move.magnitude);
    }

    // --- THIS IS THE UPDATED METHOD ---
    private void Turn()
    {
        float mouseXDelta = mouseX * mouseSensitivity * Time.deltaTime;
        float mouseYDelta = mouseY * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseYDelta;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraTarget != null)
        {
            // We set the camera's local yaw (Y-axis) to 0.
            // This stops it from fighting with the player body's rotation.
            cameraTarget.localRotation = Quaternion.Slerp(
                cameraTarget.localRotation,
                Quaternion.Euler(xRotation + currentRecoil.y, 0, 0),
                Time.deltaTime * rotSpeed
            );
        }

        // The player body handles ALL left/right rotation
        transform.Rotate(Vector3.up * mouseXDelta);
    }
    // --- END OF UPDATED METHOD ---

    private float VerticalForceCalculation()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        return verticalVelocity;
    }

    private void InputManagement()
    {
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
    }

    public void ApplyAimRecoil(GunData gunData)
    {
        float recoilX = Random.Range(-gunData.a_maxRecoil.x, gunData.a_maxRecoil.x) * gunData.a_recoilAmount;
        float recoilY = Random.Range(-gunData.a_maxRecoil.y, gunData.a_maxRecoil.y) * gunData.a_recoilAmount;
        targetRecoil += new Vector3(recoilX, recoilY, 0);
        currentRecoil = Vector3.MoveTowards(currentRecoil, targetRecoil, Time.deltaTime * gunData.a_recoilSpeed);
    }

    public void ResetAimRecoil(GunData gunData)
    {
        currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, Time.deltaTime * gunData.a_resetRecoilSpeed);
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, Time.deltaTime * gunData.a_resetRecoilSpeed);
    }

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

    private void HandleFootsteps()
    {
        if (!controller.isGrounded) return;
        if (moveInput == 0 && turnInput == 0) return;
        if (footstepSource == null || footstepClips.Length == 0) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            footstepSource.PlayOneShot(clip);
            footstepTimer = baseStepSpeed;
        }
    }

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

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"💥 Player took {damageAmount} damage! Health = {currentHealth}");

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        // 🔹 Flash and shake when hit
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

    void Die()
    {
        deathScreen.showDeadScreen = true;
        Debug.Log("Player is dead!");
    }
}