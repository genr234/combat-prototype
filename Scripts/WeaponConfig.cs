using UnityEngine;

public enum FireMode
{
    Single,
    Burst,
    Auto
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
    [Header("Base Stats")]
    public string weaponName = "New Weapon";
    public int damage = 10;
    public float cooldown = 0.5f;
    public float range = 20f;
    public float projectileRadius = 0.5f;

    [Header("Fire Behavior")]
    public FireMode fireMode = FireMode.Single;
    public int burstCount = 3;
    public float burstFireRate = 0.1f;
    public float maxSpread = 5f;
    [Range(0f, 1f)]
    public float spreadReductionWhileLocked = 0.8f;

    [Header("Ammo System")]
    public int magazineSize = 12;
    public int maxAmmo = 120;
    public float reloadTime = 1.5f;
    public bool infiniteAmmo = true;

    [Header("Special Properties")]
    [Tooltip("Number of enemies the projectile can pass through (0 = no penetration)")]
    public int penetrationCount = 0;
    [Tooltip("Can the projectile bounce off surfaces?")]
    public bool hasBounce = false;
    public int bounceCount = 0;
    [Tooltip("Explosion radius (0 = no explosion)")]
    public float explosionRadius = 0f;
    public float explosionDamage = 0f;

    [Header("Visual Effects")]
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
    public GameObject projectileTrailPrefab;
    public Color weaponColor = Color.yellow;
    public Color muzzleFlashColor = new Color(1f, 0.8f, 0.2f);

    [Header("Screen Feedback")]
    public float screenShakeForce = 0.1f;
    public float screenShakeDuration = 0.1f;
    public float recoilForce = 0.05f;
    public float recoilDuration = 0.1f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptyClipSound;
    [Range(0f, 1f)]
    public float shootVolume = 0.7f;

    [Header("Fun Parameters")]
    [Tooltip("Projectile speed multiplier for visual trail")]
    public float projectileSpeed = 50f;
    [Tooltip("Chance for critical hit (0-1)")]
    [Range(0f, 1f)]
    public float criticalChance = 0.1f;
    [Tooltip("Critical hit damage multiplier")]
    public float criticalMultiplier = 2f;
    [Tooltip("Knockback force applied to enemies")]
    public float knockbackForce = 5f;
    [Tooltip("Slow-mo effect duration on hit (0 = disabled)")]
    public float hitStopDuration = 0f;

    /// <summary>
    /// Calculate actual spread based on lock-on strength
    /// </summary>
    public float GetSpread(float lockOnStrength)
    {
        return Mathf.Lerp(maxSpread, maxSpread * (1f - spreadReductionWhileLocked), lockOnStrength);
    }

    /// <summary>
    /// Calculate damage with critical hit chance
    /// </summary>
    public int CalculateDamage(out bool isCritical)
    {
        isCritical = Random.value < criticalChance;
        return isCritical ? Mathf.RoundToInt(damage * criticalMultiplier) : damage;
    }
}