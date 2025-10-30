using UnityEngine;
using TMPro; // ✅ Needed for TextMeshPro
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

    [Header("UI Elements (TextMeshPro)")]
    [SerializeField] private TMP_Text ammoText;        // TMP_Text for ammo display
    [SerializeField] private TMP_Text reloadingText;   // TMP_Text for "Reloading..." message

    [Header("Sounds")]
    [SerializeField] private AudioClip gunShotSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptySound;

    private int currentAmmo;
    private int reserveAmmo;
    private bool isReloading = false;
    private float nextTimeToFire = 0f;

    private void Start()
    {
        currentAmmo = magazineSize;
        reserveAmmo = magazineSize * totalMagazines;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (reloadingText != null)
            reloadingText.gameObject.SetActive(false);

        UpdateAmmoUI();
    }

    private void Update()
    {
        if (isReloading) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentAmmo < magazineSize && reserveAmmo > 0)
                StartCoroutine(Reload());
            return;
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                FireWeapon();
                nextTimeToFire = Time.time + fireRate;
            }
            else
            {
                PlaySound(emptySound);
                Debug.Log("Out of ammo! Press R to reload.");
            }
        }

        UpdateAmmoUI();
    }

    void FireWeapon()
    {
        if (muzzleFlash != null)
        {
            GameObject flash = Instantiate(muzzleFlash, muzzleFlashPoint.position, muzzleFlashPoint.rotation);
            Destroy(flash, 0.2f);
        }

        currentAmmo--;
        PlaySound(gunShotSound);
        UpdateAmmoUI();

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, shootDistance))
        {
            ZombieAI zombieAI = hit.collider.GetComponent<ZombieAI>();
            if (zombieAI != null)
            {
                zombieAI.TakeDamage(damagePerShot);
                if (bloodEffect != null)
                {
                    ParticleSystem blood = Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(blood.gameObject, blood.main.duration);
                }
            }

            if (bulletHolePrefab != null)
            {
                Vector3 spawnPos = hit.point + hit.normal * 0.01f;
                GameObject hole = Instantiate(bulletHolePrefab, spawnPos, Quaternion.LookRotation(hit.normal));
                hole.transform.SetParent(hit.collider.transform);
                Destroy(hole, 5f);
            }
        }

        if (bulletTrailPrefab != null)
        {
            GameObject trail = Instantiate(bulletTrailPrefab, ShootPoint.position, Quaternion.identity);
            StartCoroutine(MoveTrail(trail, ray.origin + ray.direction * shootDistance));
        }
    }

    private IEnumerator MoveTrail(GameObject trail, Vector3 target)
    {
        float time = 0;
        Vector3 startPos = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPos, target, time);
            time += Time.deltaTime * 10f;
            yield return null;
        }

        trail.transform.position = target;
        Destroy(trail, 0.2f);
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        PlaySound(reloadSound);
        Debug.Log("Reloading...");
        if (reloadingText != null)
            reloadingText.gameObject.SetActive(true);

        yield return new WaitForSeconds(reloadTime);

        int neededAmmo = magazineSize - currentAmmo;

        if (reserveAmmo >= neededAmmo)
        {
            currentAmmo += neededAmmo;
            reserveAmmo -= neededAmmo;
        }
        else
        {
            currentAmmo += reserveAmmo;
            reserveAmmo = 0;
        }

        isReloading = false;
        if (reloadingText != null)
            reloadingText.gameObject.SetActive(false);

        UpdateAmmoUI();
        Debug.Log("Reloaded!");
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = $"{currentAmmo} / {reserveAmmo}";
    }
}
