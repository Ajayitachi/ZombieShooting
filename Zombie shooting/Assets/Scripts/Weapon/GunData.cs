using UnityEngine;

[CreateAssetMenu(fileName = "GunData", menuName = "Guns/Gun Data")]
public class GunData : ScriptableObject
{
    [Header("Gun Info")]
    public string gunName = "New Gun";

    [Header("Ammo Settings")]
    public int magazineSize = 10;
    public int totalMagazines = 3;
    public float reloadTime = 2f;

    [Header("Gun Stats")]
    public float fireRate = 0.2f;
    public float shootDistance = 50f;
    public int damagePerShot = 10;

    [Header("Sounds")]
    public AudioClip gunShotSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    [Header("Recoil Settings")]
    public Vector2 a_maxRecoil = new Vector2(0.4f, 0.7f);
    public float a_recoilAmount = 1f;
    public float a_recoilSpeed = 6f;
    public float a_resetRecoilSpeed = 4f;
}
