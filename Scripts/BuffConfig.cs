using UnityEngine;

public enum BuffType
{
    DamageBoost,
    SpeedBoost,
    HealthBoost,
    MaxHealthBoost,
    FireRateBoost,
    CriticalChanceBoost,
    CriticalDamageBoost,
    LifeSteal,
    ExplosiveShots,
    PenetrationShots,
    MultiShot
}

public enum BuffRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "NewBuff", menuName = "Roguelike/Buff Config")]
public class BuffConfig : ScriptableObject
{
    [Header("Identity")]
    public string buffName = "New Buff";
    [TextArea(2, 4)]
    public string description = "A powerful buff";
    public Sprite icon;
    
    [Header("Type & Rarity")]
    public BuffType buffType = BuffType.DamageBoost;
    public BuffRarity rarity = BuffRarity.Common;
    
    [Header("Values")]
    [Tooltip("Flat value added (e.g., +10 damage, +20 health)")]
    public float flatBonus = 0f;
    [Tooltip("Percentage multiplier (e.g., 0.1 = +10% damage)")]
    public float percentageBonus = 0f;
    
    [Header("Stacking")]
    public bool canStack = true;
    public int maxStacks = 5;
    
    [Header("Visual")]
    public Color buffColor = Color.green;
    
    /// <summary>
    /// Get the color based on rarity
    /// </summary>
    public Color GetRarityColor()
    {
        return rarity switch
        {
            BuffRarity.Common => new Color(0.8f, 0.8f, 0.8f),      // Gray
            BuffRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),    // Green
            BuffRarity.Rare => new Color(0.2f, 0.4f, 1f),          // Blue
            BuffRarity.Epic => new Color(0.6f, 0.2f, 0.8f),        // Purple
            BuffRarity.Legendary => new Color(1f, 0.6f, 0f),       // Orange
            _ => Color.white
        };
    }
    
    /// <summary>
    /// Get formatted description with values
    /// </summary>
    public string GetFormattedDescription()
    {
        var desc = description;
        
        if (flatBonus != 0)
        {
            desc = desc.Replace("{flat}", flatBonus.ToString("F0"));
        }
        if (percentageBonus != 0)
        {
            desc = desc.Replace("{percent}", (percentageBonus * 100f).ToString("F0"));
        }
        
        return desc;
    }
}

