using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds all available rewards and handles random selection
/// </summary>
[CreateAssetMenu(fileName = "RewardPool", menuName = "Roguelike/Reward Pool")]
public class RewardPool : ScriptableObject
{
    [Header("Available Rewards")]
    public List<BuffConfig> availableBuffs = new List<BuffConfig>();
    public List<WeaponConfig> availableWeapons = new List<WeaponConfig>();

    [Header("Selection Settings")]
    [Range(2, 5)]
    public int numberOfChoices = 3;

    [Range(0f, 1f)]
    [Tooltip("Chance that a choice will be a weapon instead of a buff")]
    public float weaponChance = 0.3f;

    [Header("Rarity Weights")]
    public float commonWeight = 50f;
    public float uncommonWeight = 30f;
    public float rareWeight = 15f;
    public float epicWeight = 4f;
    public float legendaryWeight = 1f;

    /// <summary>
    /// Get random reward choices
    /// </summary>
    public List<RewardChoice> GetRandomRewards()
    {
        var choices = new List<RewardChoice>();

        for (var i = 0; i < numberOfChoices; i++)
        {
            var isWeapon = Random.value < weaponChance && availableWeapons.Count > 0;

            if (isWeapon)
            {
                var weapon = GetRandomWeapon();
                if (weapon != null)
                {
                    choices.Add(new RewardChoice(weapon));
                }
            }
            else if (availableBuffs.Count > 0)
            {
                var buff = GetRandomBuff();
                if (buff != null)
                {
                    choices.Add(new RewardChoice(buff));
                }
            }
        }

        // Fallback if no choices generated
        if (choices.Count == 0 && availableBuffs.Count > 0)
        {
            choices.Add(new RewardChoice(availableBuffs[0]));
        }

        return choices;
    }

    private BuffConfig GetRandomBuff()
    {
        if (availableBuffs.Count == 0) return null;

        // Select rarity based on weights
        var targetRarity = SelectRarityByWeight();

        // Get buffs of that rarity
        var candidates = availableBuffs.FindAll(b => b.rarity == targetRarity);

        // Fallback to any buff if no matches
        if (candidates.Count == 0)
        {
            candidates = availableBuffs;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private WeaponConfig GetRandomWeapon()
    {
        if (availableWeapons.Count == 0) return null;
        return availableWeapons[Random.Range(0, availableWeapons.Count)];
    }

    private BuffRarity SelectRarityByWeight()
    {
        var totalWeight = commonWeight + uncommonWeight + rareWeight + epicWeight + legendaryWeight;
        var roll = Random.Range(0f, totalWeight);

        var current = 0f;

        current += commonWeight;
        if (roll < current) return BuffRarity.Common;

        current += uncommonWeight;
        if (roll < current) return BuffRarity.Uncommon;

        current += rareWeight;
        if (roll < current) return BuffRarity.Rare;

        current += epicWeight;
        if (roll < current) return BuffRarity.Epic;

        return BuffRarity.Legendary;
    }
}

