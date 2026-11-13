using UnityEngine;

/// <summary>
/// Procedural weapon sway, walk bob and recoil.
/// The old version *added* bob/recoil to the position every frame, so offsets accumulated
/// and the gun drifted; now every frame the pose is rebuilt from the rest pose.
/// </summary>
public class WeaponAnim : MonoBehaviour
{
    [Header("Weapon Sway")]
    [SerializeField] private float positionalSway = 0.1f;
    [SerializeField] private float rotationalSway = 0.1f;
    [SerializeField] private float swaySmoothness = 1f;
    [SerializeField] private float maxSwayOffset = 0.06f;

    private Vector3 initinalPosition = Vector3.zero;
    private Quaternion initinalRotation = Quaternion.identity;

    [Header("Weapon Bobbing")]
    [SerializeField] private float bobbingSpeed = 5f;
    [SerializeField] private float bobbingAmount = 5f;

    [Header("Recoil Animation")]
    [SerializeField] private float recoilAmount = 0.2f;
    [SerializeField] private float rocoilSoothness = 5f;
    [SerializeField] private float recoilRotation = 6f;

    [HideInInspector] public bool isRecoiling = false;

    private Vector3 swayPos;
    private Quaternion swayRot = Quaternion.identity;
    private float bobTimer;
    private Vector3 bobPos;
    private float recoil;        // 0..1 kick amount, decays over time
    private PlayerController player;

    private void Start()
    {
        player = GetComponentInParent<PlayerController>();
        initinalPosition = transform.localPosition;
        initinalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (!GameState.CanPlay) return;

        UpdateSway();
        UpdateBobbing();
        UpdateRecoil();

        Vector3 recoilPos = new Vector3(0f, recoil * recoilAmount * 0.15f, -recoil * recoilAmount);
        Quaternion recoilRot = Quaternion.Euler(-recoil * recoilRotation, 0f, 0f);

        transform.localPosition = initinalPosition + swayPos + bobPos + recoilPos;
        // Rotations are applied in the holder's (view) space, not the model's own axes:
        // the pistol mesh is rotated ~87°, so a local X rotation used to roll it instead of kicking the muzzle up.
        transform.localRotation = recoilRot * swayRot * initinalRotation;
    }

    /// <summary>Trigger the recoil kick (called by Shoot on every shot).</summary>
    public void Kick()
    {
        recoil = 1f;
        isRecoiling = true;
    }

    private void UpdateSway()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        Vector3 targetPos = Vector3.ClampMagnitude(new Vector3(-mouseX, -mouseY, 0f) * positionalSway * 0.1f, maxSwayOffset);
        Quaternion targetRot = Quaternion.Euler(new Vector3(-mouseY, mouseX, mouseX) * rotationalSway * 10f);

        float k = 1f - Mathf.Exp(-Mathf.Max(0.1f, swaySmoothness) * Time.deltaTime);
        swayPos = Vector3.Lerp(swayPos, targetPos, k);
        swayRot = Quaternion.Slerp(swayRot, targetRot, k);
    }

    private void UpdateBobbing()
    {
        float move = player != null ? player.MoveAmount : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        bool grounded = player == null || player.controller == null || player.controller.isGrounded;

        // bobbingAmount used to be applied per frame; scale it into a sensible range.
        float amount = bobbingAmount > 0.5f ? bobbingAmount * 0.002f : bobbingAmount * 2f;

        if (move > 0.1f && grounded)
        {
            bobTimer += Time.deltaTime * bobbingSpeed * Mathf.Max(1f, move);
            Vector3 target = new Vector3(Mathf.Cos(bobTimer) * amount, -Mathf.Abs(Mathf.Sin(bobTimer)) * amount, 0f) * Mathf.Min(move, 1.6f);
            bobPos = Vector3.Lerp(bobPos, target, Time.deltaTime * 12f);
        }
        else
        {
            bobPos = Vector3.Lerp(bobPos, Vector3.zero, Time.deltaTime * 6f);
        }
    }

    private void UpdateRecoil()
    {
        recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * Mathf.Max(1f, rocoilSoothness));
        if (recoil <= 0.01f) isRecoiling = false;
    }
}
