using UnityEngine;
using TMPro;
using System.Collections;

public class Shoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform ShootPoint;
    [SerializeField] private Transform muzzleFlashPoint;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private ParticleSystem bloodEffect;
    [SerializeField] private GameObject bulletTrailPrefab;
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioSource audioSource;

    [Header("Gun Settings")]
    [SerializeField] private int magazineSize = 10;
    [SerializeField] private int totalMagazines = 3; // number of extra mags
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float shootDistance = 50f;
    [SerializeField] private int damagePerShot = 10;
    [SerializeField] private bool automatic = false;
    [SerializeField] private float headshotMultiplier = 2.5f;
    [Tooltip("Top fraction of a zombie's collider that counts as the head.")]
    [SerializeField, Range(0.05f, 0.4f)] private float headZone = 0.18f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Effects")]
    [Tooltip("Tracer, impact sparks and bullet holes generated in code when no prefab is assigned.")]
    [SerializeField] private bool proceduralEffects = true;
    [SerializeField, Range(0.1f, 2f)] private float muzzleFlashScale = 0.5f;

    [Header("Feel")]
    [SerializeField] private float recoilPitch = 1.6f;
    [SerializeField] private float recoilYaw = 0.5f;
    [SerializeField] private float shotShake = 0.025f;

    [Header("UI Elements (TextMeshPro)")]
    [SerializeField] private TMP_Text ammoText;        // ammo display
    [SerializeField] private TMP_Text reloadingText;   // "Reloading..." message

    [Header("Sounds")]
    [SerializeField] private AudioClip gunShotSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptySound;

    private int currentAmmo;
    private int reserveAmmo;
    private bool isReloading = false;
    private float nextTimeToFire = 0f;

    private WeaponAnim weaponAnim;
    private MouseLook mouseLook;
    private PlayerController player;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => isReloading;
    public float ReloadProgress { get; private set; }

    /// <summary>Raised every time a bullet is fired.</summary>
    public event System.Action Fired;

    private void Start()
    {
        currentAmmo = magazineSize;
        reserveAmmo = magazineSize * totalMagazines;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (playerCamera == null) playerCamera = Camera.main;

        weaponAnim = GetComponentInChildren<WeaponAnim>();
        if (weaponAnim == null) weaponAnim = GetComponentInParent<WeaponAnim>();
        mouseLook = GetComponentInParent<MouseLook>();
        if (mouseLook == null && playerCamera != null) mouseLook = playerCamera.GetComponentInParent<MouseLook>();
        player = GetComponentInParent<PlayerController>();

        if (reloadingText != null)
            reloadingText.gameObject.SetActive(false);

        UpdateAmmoUI();
    }

    private void Update()
    {
        // No shooting while paused, dead, or on an end screen (clicking a menu button used to fire).
        if (!GameState.CanPlay || (player != null && player.IsDead)) return;
        if (isReloading) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
            return;
        }

        bool trigger = automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (trigger && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                FireWeapon();
                nextTimeToFire = Time.time + fireRate;
            }
            else if (reserveAmmo > 0)
            {
                TryReload(); // auto-reload on empty click
            }
            else if (Input.GetMouseButtonDown(0))
            {
                PlaySound(emptySound);
            }
        }
    }

    private void TryReload()
    {
        if (currentAmmo < magazineSize && reserveAmmo > 0)
            StartCoroutine(Reload());
    }

    private void FireWeapon()
    {
        if (muzzleFlash != null && muzzleFlashPoint != null)
        {
            GameObject flash = Instantiate(muzzleFlash, muzzleFlashPoint.position, muzzleFlashPoint.rotation, muzzleFlashPoint);
            // The WarFX flash prefab has a burst count of 0 and only emits on a 10/s timer,
            // so it rarely appeared before being destroyed. Emit its particles immediately.
            flash.transform.localScale = Vector3.one * muzzleFlashScale; // rifle-sized effect → pistol-sized
            foreach (var ps in flash.GetComponentsInChildren<ParticleSystem>())
                ps.Emit(1);
            Destroy(flash, 0.2f);
        }

        currentAmmo--;
        PlaySound(gunShotSound, Random.Range(0.94f, 1.06f));
        UpdateAmmoUI();

        // Feel: weapon kick, camera recoil and a tiny shake.
        if (weaponAnim != null) weaponAnim.Kick();
        if (mouseLook != null) mouseLook.AddRecoil(recoilPitch, recoilYaw);
        if (player != null) player.ShakeCamera(shotShake, 0.08f);
        Fired?.Invoke();

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 endPoint = ray.origin + ray.direction * shootDistance;

        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            ZombieAI zombieAI = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombieAI != null)
            {
                bool headshot = IsHeadshot(zombieAI, hit.point);
                int dmg = headshot ? Mathf.RoundToInt(damagePerShot * headshotMultiplier) : damagePerShot;
                zombieAI.TakeDamage(dmg, headshot);

                if (bloodEffect != null)
                {
                    ParticleSystem blood = Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(blood.gameObject, Mathf.Max(1f, blood.main.duration + blood.main.startLifetime.constantMax));
                }
            }
            else
            {
                // Bullet holes only on the world, never floating on zombies.
                if (bulletHolePrefab != null)
                {
                    Vector3 spawnPos = hit.point + hit.normal * 0.01f;
                    GameObject hole = Instantiate(bulletHolePrefab, spawnPos, Quaternion.LookRotation(hit.normal));
                    hole.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
                    if (hit.rigidbody == null || hit.rigidbody.isKinematic)
                        hole.transform.SetParent(hit.collider.transform, true);
                    Destroy(hole, 10f);
                }
                else if (proceduralEffects)
                {
                    WeaponFX.SpawnBulletHole(hit);
                }
                if (proceduralEffects) WeaponFX.SpawnImpactSparks(hit.point, hit.normal);

                if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                    hit.rigidbody.AddForceAtPosition(ray.direction * 4f, hit.point, ForceMode.Impulse);
            }
        }

        Vector3 tracerStart = muzzleFlashPoint != null ? muzzleFlashPoint.position : ShootPoint != null ? ShootPoint.position : ray.origin;
        if (bulletTrailPrefab != null && ShootPoint != null)
        {
            GameObject trail = Instantiate(bulletTrailPrefab, ShootPoint.position, Quaternion.LookRotation(endPoint - ShootPoint.position));
            StartCoroutine(MoveTrail(trail, endPoint));
        }
        else if (proceduralEffects)
        {
            WeaponFX.SpawnTracer(tracerStart, endPoint);
        }
    }

    private bool IsHeadshot(ZombieAI zombie, Vector3 point)
    {
        Collider col = zombie.Capsule;
        if (col == null) return false;
        Bounds b = col.bounds;
        return point.y >= b.max.y - b.size.y * headZone;
    }

    private IEnumerator MoveTrail(GameObject trail, Vector3 target)
    {
        Vector3 startPos = trail.transform.position;
        float distance = Vector3.Distance(startPos, target);
        const float speed = 300f; // m/s
        float t = 0f;
        while (t < 1f && trail != null)
        {
            t += Time.deltaTime * speed / Mathf.Max(0.01f, distance);
            trail.transform.position = Vector3.Lerp(startPos, target, t);
            yield return null;
        }
        if (trail != null) Destroy(trail, 0.15f);
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        ReloadProgress = 0f;
        PlaySound(reloadSound);
        if (reloadingText != null) reloadingText.gameObject.SetActive(true);

        float t = 0f;
        while (t < reloadTime)
        {
            t += Time.deltaTime;
            ReloadProgress = t / reloadTime;
            yield return null;
        }

        int neededAmmo = magazineSize - currentAmmo;
        int loaded = Mathf.Min(neededAmmo, reserveAmmo);
        currentAmmo += loaded;
        reserveAmmo -= loaded;

        isReloading = false;
        ReloadProgress = 1f;
        if (reloadingText != null) reloadingText.gameObject.SetActive(false);
        UpdateAmmoUI();
    }

    /// <summary>Restocks spare magazines (called between waves).</summary>
    public void RefillReserve()
    {
        reserveAmmo = Mathf.Max(reserveAmmo, magazineSize * totalMagazines);
        UpdateAmmoUI();
    }

    private void PlaySound(AudioClip clip, float pitch = 1f)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip);
    }

    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        bool low = currentAmmo <= Mathf.CeilToInt(magazineSize * 0.25f);
        string magColor = currentAmmo == 0 ? "#FF4040" : low ? "#FFB040" : "#FFFFFF";
        ammoText.text = $"<color={magColor}>{currentAmmo}</color><size=70%><color=#9AA5A0> / {reserveAmmo}</color></size>";
    }
}
