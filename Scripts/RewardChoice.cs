using UnityEngine;

/// <summary>
/// Represents a single reward choice (either a buff or a weapon)
/// </summary>
[System.Serializable]
public class RewardChoice
{
    public bool isBuff;
    public BuffConfig buff;
    public WeaponConfig weapon;

    public RewardChoice(BuffConfig buff)
    {
        this.isBuff = true;
        this.buff = buff;
        this.weapon = null;
    }

    public RewardChoice(WeaponConfig weapon)
    {
        this.isBuff = false;
        this.buff = null;
        this.weapon = weapon;
    }

    public string GetName()
    {
        return isBuff ? buff.buffName : weapon.weaponName;
    }

    public string GetDescription()
    {
        if (isBuff)
        {
            var desc = buff.description;
            desc = desc.Replace("{percent}", (buff.percentageBonus * 100).ToString("F0"));
            desc = desc.Replace("{flat}", buff.flatBonus.ToString("F0"));
            return desc;
        }
        else
        {
            return $"Damage: {weapon.damage} | Fire Rate: {(1f / weapon.cooldown).ToString("F1")}/s";
        }
    }

    public Color GetColor()
    {
        if (isBuff)
        {
            return buff.rarity switch
            {
                BuffRarity.Common => new Color(0.7f, 0.7f, 0.7f),
                BuffRarity.Uncommon => new Color(0.3f, 0.8f, 0.3f),
                BuffRarity.Rare => new Color(0.3f, 0.5f, 1f),
                BuffRarity.Epic => new Color(0.7f, 0.3f, 0.9f),
                BuffRarity.Legendary => new Color(1f, 0.6f, 0.1f),
                _ => Color.white
            };
        }
        else
        {
            return new Color(1f, 0.8f, 0.2f);
        }
    }
}


