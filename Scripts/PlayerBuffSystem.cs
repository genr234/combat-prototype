using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active buffs on the player
/// </summary>
public class PlayerBuffSystem : MonoBehaviour
{
    [Header("Active Buffs")]
    [SerializeField] private List<BuffConfig> activeBuffs = new List<BuffConfig>();

    // Cached stat multipliers
    private float damageMultiplier = 1f;
    private float speedMultiplier = 1f;
    private float fireRateMultiplier = 1f;
    private float critChanceBonus = 0f;
    private float critDamageMultiplier = 1f;
    private float lifeStealPercent = 0f;
    private bool hasExplosiveShots = false;
    private bool hasPenetrationShots = false;
    private int multiShotCount = 0;

    public float DamageMultiplier => damageMultiplier;
    public float SpeedMultiplier => speedMultiplier;
    public float FireRateMultiplier => fireRateMultiplier;
    public float CritChanceBonus => critChanceBonus;
    public float CritDamageMultiplier => critDamageMultiplier;
    public float LifeStealPercent => lifeStealPercent;
    public bool HasExplosiveShots => hasExplosiveShots;
    public bool HasPenetrationShots => hasPenetrationShots;
    public int MultiShotCount => multiShotCount;

    private void Update()
    {
        // Sync speed multiplier with PlayerController
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.speedMultiplier = speedMultiplier;
        }
    }

    /// <summary>
    /// Add a buff to the player
    /// </summary>
    public void AddBuff(BuffConfig buff)
    {
        if (buff == null) return;

        activeBuffs.Add(buff);
        ApplyBuff(buff);
        RecalculateStats();

        Debug.Log($"[PlayerBuffSystem] Added buff: {buff.buffName}");
    }

    /// <summary>
    /// Apply immediate buff effects
    /// </summary>
    private void ApplyBuff(BuffConfig buff)
    {
        switch (buff.buffType)
        {
            case BuffType.HealthBoost:
                var playerManager = GetComponent<PlayerManager>();
                if (playerManager != null)
                {
                    playerManager.Health = Mathf.Min(playerManager.Health + (int)buff.flatBonus, playerManager.maxHealth);
                }
                break;

            case BuffType.MaxHealthBoost:
                var playerMgr = GetComponent<PlayerManager>();
                if (playerMgr != null)
                {
                    playerMgr.maxHealth += (int)buff.flatBonus;
                    playerMgr.Health += (int)buff.flatBonus;  // Also heal by the same amount
                }
                break;
        }
    }

    /// <summary>
    /// Recalculate all stat multipliers
    /// </summary>
    private void RecalculateStats()
    {
        // Reset
        damageMultiplier = 1f;
        speedMultiplier = 1f;
        fireRateMultiplier = 1f;
        critChanceBonus = 0f;
        critDamageMultiplier = 1f;
        lifeStealPercent = 0f;
        hasExplosiveShots = false;
        hasPenetrationShots = false;
        multiShotCount = 0;

        // Apply all buffs
        foreach (var buff in activeBuffs)
        {
            switch (buff.buffType)
            {
                case BuffType.DamageBoost:
                    damageMultiplier += buff.percentageBonus;
                    break;
                case BuffType.SpeedBoost:
                    speedMultiplier += buff.percentageBonus;
                    break;
                case BuffType.FireRateBoost:
                    fireRateMultiplier += buff.percentageBonus;
                    break;
                case BuffType.CriticalChanceBoost:
                    critChanceBonus += buff.percentageBonus;
                    break;
                case BuffType.CriticalDamageBoost:
                    critDamageMultiplier += buff.percentageBonus;
                    break;
                case BuffType.LifeSteal:
                    lifeStealPercent += buff.percentageBonus;
                    break;
                case BuffType.ExplosiveShots:
                    hasExplosiveShots = true;
                    break;
                case BuffType.PenetrationShots:
                    hasPenetrationShots = true;
                    break;
                case BuffType.MultiShot:
                    multiShotCount += (int)buff.flatBonus;
                    break;
            }
        }
    }

    /// <summary>
    /// Get all active buffs
    /// </summary>
    public List<BuffConfig> GetActiveBuffs()
    {
        return new List<BuffConfig>(activeBuffs);
    }

    /// <summary>
    /// Clear all buffs (for debugging/reset)
    /// </summary>
    public void ClearBuffs()
    {
        activeBuffs.Clear();
        RecalculateStats();
    }

    /// <summary>
    /// Calculate final damage with all modifiers
    /// </summary>
    public int CalculateDamage(int baseDamage, out bool isCritical)
    {
        var damage = baseDamage * damageMultiplier;

        // Check for critical hit
        isCritical = Random.value < critChanceBonus;
        if (isCritical)
        {
            damage *= critDamageMultiplier;
        }

        return Mathf.RoundToInt(damage);
    }

    /// <summary>
    /// Process life steal healing
    /// </summary>
    public void ProcessLifeSteal(int damageDealt)
    {
        if (lifeStealPercent <= 0) return;

        var healAmount = damageDealt * lifeStealPercent;
        if (healAmount <= 0) return;

        var playerManager = GetComponent<PlayerManager>();
        if (playerManager != null)
        {
            playerManager.Health = Mathf.Min(playerManager.Health + Mathf.RoundToInt(healAmount), playerManager.maxHealth);
        }
    }

    /// <summary>
    /// Bonus penetration count from buffs
    /// </summary>
    public int PenetrationBonus => hasPenetrationShots ? 999 : 0;

    /// <summary>
    /// Bonus explosion radius from buffs
    /// </summary>
    public float ExplosionRadiusBonus => hasExplosiveShots ? 3f : 0f;
}

