using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// First-person look. Yaw turns the player body; pitch (up/down) tilts every transform the view
/// actually depends on: the Cinemachine follow target (Cinemachine overwrites the Camera itself),
/// the weapon holder (so the gun follows your aim) and the camera when nothing else drives it.
/// </summary>
public class MouseLook : MonoBehaviour
{
    public const string SensitivityPrefKey = "MouseSensitivity";

    [Header("Mouse Settings")]
    [Tooltip("100 = the original feel at 60 FPS; now frame-rate independent.")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Recoil")]
    [SerializeField] private float recoilReturnSpeed = 8f;

    [Header("References")]
    [SerializeField] private Transform playerBody; // the Player root (yaw)
    [Tooltip("Optional. Transforms to tilt up/down. Left empty they are found automatically.")]
    [SerializeField] private Transform[] pitchTargets;
    [Tooltip("Also tilt the object this script is on (off when it sits on the player root).")]
    [SerializeField] private bool pitchSelf = true;

    private float xRotation;
    private Vector2 recoil, recoilTarget;
    private readonly List<Transform> pitched = new List<Transform>();
    private bool initialized;

    /// <summary>Current view pitch in degrees (negative = looking up).</summary>
    public float Pitch => xRotation;

    /// <summary>Used by the bootstrap when a scene has no look controller.</summary>
    public void Configure(Transform body, bool tiltSelf)
    {
        playerBody = body;
        pitchSelf = tiltSelf;
    }

    private void Start()
    {
        if (playerBody == null)
        {
            var pc = GetComponentInParent<PlayerController>();
            playerBody = pc != null ? pc.transform : transform.root;
        }
        CollectPitchTargets();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        initialized = true;
    }

    private void CollectPitchTargets()
    {
        pitched.Clear();
        if (pitchSelf && transform != playerBody) pitched.Add(transform);

        if (pitchTargets != null)
            foreach (var t in pitchTargets) AddTarget(t);

        // 1) What Cinemachine follows (it copies that rotation onto the real camera).
        Transform cmFollow = null;
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
        {
            if (mb == null || mb.GetType().Name != "CinemachineCamera") continue;
            var prop = mb.GetType().GetProperty("Follow");
            var follow = prop != null ? prop.GetValue(mb) as Transform : null;
            if (follow != null && playerBody != null && follow.IsChildOf(playerBody)) { cmFollow = follow; AddTarget(follow); }
        }

        if (playerBody != null)
        {
            foreach (var t in playerBody.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                // 2) Follow targets by name, 3) the weapon holder so the gun follows the aim.
                if (n.StartsWith("follow") || n.Contains("weapon holder") || n == "weaponholder")
                    AddTarget(t);
            }

            // 4) A camera that nothing above tilts (no Cinemachine, not under a pitched transform).
            var cam = playerBody.GetComponentInChildren<Camera>();
            if (cam != null && cmFollow == null && !HasCinemachineBrain(cam) && !UnderPitched(cam.transform))
                AddTarget(cam.transform);
        }
    }

    private void AddTarget(Transform t)
    {
        if (t == null || t == playerBody || pitched.Contains(t)) return;
        // Don't tilt something whose parent is already tilted (would double the angle).
        if (UnderPitched(t)) return;
        // If this is a parent of something already tilted, keep only the parent.
        pitched.RemoveAll(p => p != null && p != t && p.IsChildOf(t));
        pitched.Add(t);
    }

    private bool UnderPitched(Transform t)
    {
        foreach (var p in pitched) if (p != null && p != t && t.IsChildOf(p)) return true;
        return false;
    }

    private static bool HasCinemachineBrain(Camera cam)
    {
        foreach (var c in cam.GetComponents<Component>())
            if (c != null && c.GetType().Name == "CinemachineBrain") return true;
        return false;
    }

    /// <summary>Test/automation hook: movement added to the next frame's mouse delta (legacy axis units).</summary>
    public static Vector2 InjectedDelta;

    /// <summary>Mouse movement this frame in "legacy axis" units (pixels × 0.1).</summary>
    public static Vector2 ReadMouseDelta()
    {
        if (InjectedDelta != Vector2.zero)
        {
            Vector2 inj = InjectedDelta;
            InjectedDelta = Vector2.zero;
            return inj;
        }
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 d = Mouse.current.delta.ReadValue() * 0.1f;
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector2 legacy = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            if (legacy.sqrMagnitude > d.sqrMagnitude) d = legacy;
#endif
            return d;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#else
        return Vector2.zero;
#endif
    }

    private void Update()
    {
        if (!initialized || !GameState.CanPlay) return;

        // Click back into the game window to re-capture the mouse (Esc / alt-tab releases it).
        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float userScale = PlayerPrefs.GetFloat(SensitivityPrefKey, 1f);
        float sens = mouseSensitivity * userScale * (1f / 60f);
        Vector2 m = ReadMouseDelta();
        float mouseX = m.x * sens;
        bool invert = invertY ^ (PlayerPrefs.GetInt("InvertY", 0) == 1); // menu setting
        float mouseY = m.y * sens * (invert ? -1f : 1f);

        // Recoil eases toward its target, and the target eases back to zero.
        recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, recoilReturnSpeed * Time.deltaTime);
        Vector2 prev = recoil;
        recoil = Vector2.Lerp(recoil, recoilTarget, 20f * Time.deltaTime);
        Vector2 recoilDelta = recoil - prev;

        xRotation = Mathf.Clamp(xRotation - mouseY - recoilDelta.x, minPitch, maxPitch);
        Quaternion pitch = Quaternion.Euler(xRotation, 0f, 0f);
        foreach (var t in pitched) if (t != null) t.localRotation = pitch;

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * (mouseX + recoilDelta.y), Space.World);
    }

    /// <summary>Kicks the view up (pitch degrees) with a little random sideways drift.</summary>
    public void AddRecoil(float pitchKick, float yawJitter)
    {
        recoilTarget += new Vector2(pitchKick, Random.Range(-yawJitter, yawJitter));
    }
}
