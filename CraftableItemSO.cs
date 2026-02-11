using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Available stat types for boosts
/// </summary>
public enum StatType
{
    Health,
    Attack,
    Defense,
    Speed,
    CritChance,
    CritDamage,
    ResourceGain,
    ExperienceGain,
    CooldownReduction
}

/// <summary>
/// ScriptableObject defining a craftable item that can be socketed to the Soul Weapon
/// Features two-tier bonus system: small owned bonus + large socketed bonus
/// </summary>
[CreateAssetMenu(fileName = "New Craftable Item", menuName = "Crafting System/Craftable Item")]
public class CraftableItemSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this item")]
    public string itemId;

    [Tooltip("Display name shown to player")]
    public string displayName;

    [Tooltip("Icon for inventory/UI")]
    public Sprite icon;

    [Tooltip("Description of what this item does")]
    [TextArea(2, 5)]
    public string description;

    [Header("Progression")]
    [Tooltip("Maximum level this item can reach")]
    public int maxLevel = 10;

    [Tooltip("How many copies needed per level (uses curve)")]
    public AnimationCurve levelingCurve;

    [Header("Crafting Requirements")]
    [Tooltip("Resources needed to craft this item")]
    public List<CraftingRequirement> requirements = new List<CraftingRequirement>();

    [Header("Unlock Requirements")]
    [Tooltip("Requirements that must be met before item appears in crafting menu")]
    [SerializeField] private List<ItemCraftedRequirement> itemCraftedRequirements = new List<ItemCraftedRequirement>();
    [SerializeField] private List<ItemLevelRequirement> itemLevelRequirements = new List<ItemLevelRequirement>();
    [SerializeField] private List<ItemUnlockedRequirement> itemUnlockedRequirements = new List<ItemUnlockedRequirement>();
    [SerializeField] private List<PlayerLevelRequirement> playerLevelRequirements = new List<PlayerLevelRequirement>();

    [Header("Bonus System - OWNED (Always Active)")]
    [Tooltip("Small passive bonuses just for owning this item (active even when not socketed)")]
    public List<StatBoost> ownedBonuses = new List<StatBoost>();

    [Header("Bonus System - SOCKETED (Only When Socketed)")]
    [Tooltip("Large bonuses that only apply when this item is socketed to Soul Weapon")]
    public List<StatBoost> socketedBonuses = new List<StatBoost>();

    [Header("Soul Weapon Integration")]
    [Tooltip("Can this item be socketed to the Soul Weapon?")]
    public bool canSocketToSoulWeapon = false;

    [Tooltip("Element/affinity of this weapon for synergy bonuses")]
    public WeaponElement element = WeaponElement.None;

    [Header("Weapon Skill (Socket Only)")]
    [Tooltip("Active ability that triggers when this weapon is socketed")]
    public Ability weaponSkill;

    [Tooltip("Cooldown for weapon skill activation (seconds)")]
    public float weaponSkillCooldown = 10f;

    [Tooltip("Does this skill activate automatically or manually?")]
    public bool autoActivateSkill = true;

    [Header("Visual Feedback")]
    [Tooltip("Icon shown when owned but not socketed (normal version)")]
    public Sprite collectionIcon;

    [Tooltip("Icon shown when socketed (glowing/enhanced version)")]
    public Sprite socketedIcon;

    [Tooltip("Particle effect when weapon skill activates")]
    public GameObject skillActivationEffect;

    [Header("Rarity & Categorization")]
    [Tooltip("Rarity tier for this item")]
    public Rarity rarity = Rarity.Common;

    [Tooltip("Category/type for filtering")]
    public ItemCategory category = ItemCategory.Weapon;

    // Property to get all unlock requirements combined
    public List<UnlockRequirement> unlockRequirements
    {
        get
        {
            List<UnlockRequirement> allRequirements = new List<UnlockRequirement>();
            allRequirements.AddRange(itemCraftedRequirements);
            allRequirements.AddRange(itemLevelRequirements);
            allRequirements.AddRange(itemUnlockedRequirements);
            allRequirements.AddRange(playerLevelRequirements);
            return allRequirements;
        }
    }

    #region Description Generation

    /// <summary>
    /// Get formatted description of owned bonuses (always active)
    /// </summary>
    public string GetOwnedBonusDescription(int level = 1)
    {
        if (ownedBonuses == null || ownedBonuses.Count == 0)
            return "No collection bonuses";

        string description = "";
        foreach (var boost in ownedBonuses)
        {
            if (boost.flatBonus != 0 || boost.flatBonusPerLevel != 0)
            {
                float flatValue = boost.GetFlatBonusAtLevel(level);
                description += $"+{flatValue:F1} {boost.statType}\n";
            }

            if (boost.percentBonus != 0 || boost.percentBonusPerLevel != 0)
            {
                float percentValue = boost.GetPercentBonusAtLevel(level);
                description += $"+{percentValue:F1}% {boost.statType}\n";
            }
        }

        return description.TrimEnd('\n');
    }

    /// <summary>
    /// Get formatted description of socketed bonuses (only when socketed)
    /// </summary>
    public string GetSocketedBonusDescription(int level = 1)
    {
        if (socketedBonuses == null || socketedBonuses.Count == 0)
            return "No socket bonuses";

        string description = "";
        foreach (var boost in socketedBonuses)
        {
            if (boost.flatBonus != 0 || boost.flatBonusPerLevel != 0)
            {
                float flatValue = boost.GetFlatBonusAtLevel(level);
                description += $"+{flatValue:F1} {boost.statType}\n";
            }

            if (boost.percentBonus != 0 || boost.percentBonusPerLevel != 0)
            {
                float percentValue = boost.GetPercentBonusAtLevel(level);
                description += $"+{percentValue:F1}% {boost.statType}\n";
            }
        }

        return description.TrimEnd('\n');
    }

    /// <summary>
    /// Get complete stat boost description combining owned + socketed
    /// </summary>
    public string GetCompleteStatDescription(int level = 1)
    {
        string desc = "=== COLLECTION BONUS (Always Active) ===\n";
        desc += GetOwnedBonusDescription(level);
        desc += "\n\n=== SOCKET BONUS (When Socketed) ===\n";
        desc += GetSocketedBonusDescription(level);

        if (weaponSkill != null)
        {
            desc += $"\n\nSkill: {weaponSkill.name}";
            desc += $"\nCooldown: {weaponSkillCooldown}s";
        }

        return desc;
    }

    /// <summary>
    /// Get skill description if weapon has a skill
    /// </summary>
    public string GetWeaponSkillDescription()
    {
        if (weaponSkill == null)
            return "No active skill";

        string skillDesc = $"{weaponSkill.name}\n";
        skillDesc += $"Cooldown: {weaponSkillCooldown}s\n";

        // Add skill type description
        switch (weaponSkill.typeOfAttack)
        {
            case Type.damage:
                skillDesc += "Deals damage to enemies";
                break;
            case Type.AOE:
                skillDesc += "Area of effect damage";
                break;
            case Type.buff:
                skillDesc += "Buffs the player";
                break;
            case Type.heal:
                skillDesc += "Heals the player";
                break;
            case Type.PerEnemy:
                skillDesc += "Targets each enemy";
                break;
        }

        return skillDesc;
    }

    #endregion

    #region Unlock System

    /// <summary>
    /// Check if all unlock requirements are met
    /// </summary>
    public bool CanBeUnlocked()
    {
        if (unlockRequirements == null || unlockRequirements.Count == 0)
            return true;

        foreach (var requirement in unlockRequirements)
        {
            if (!requirement.IsMet())
                return false;
        }

        return true;
    }

    /// <summary>
    /// Get list of unmet requirements for UI display
    /// </summary>
    public List<string> GetUnmetRequirements()
    {
        List<string> unmetRequirements = new List<string>();

        if (unlockRequirements == null || unlockRequirements.Count == 0)
            return unmetRequirements;

        foreach (var requirement in unlockRequirements)
        {
            if (!requirement.IsMet())
            {
                unmetRequirements.Add(requirement.GetDescription());
            }
        }

        return unmetRequirements;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the icon to display based on socket status
    /// </summary>
    public Sprite GetDisplayIcon(bool isSocketed)
    {
        if (isSocketed && socketedIcon != null)
            return socketedIcon;

        if (collectionIcon != null)
            return collectionIcon;

        return icon;
    }

    /// <summary>
    /// Check if this item has any socketed bonuses
    /// </summary>
    public bool HasSocketedBonuses()
    {
        return socketedBonuses != null && socketedBonuses.Count > 0;
    }

    /// <summary>
    /// Check if this item has an active weapon skill
    /// </summary>
    public bool HasWeaponSkill()
    {
        return weaponSkill != null;
    }

    /// <summary>
    /// Get total owned bonus value for a specific stat type
    /// </summary>
    public float GetTotalOwnedBonus(StatType statType, int level = 1)
    {
        float total = 0f;

        if (ownedBonuses == null)
            return total;

        foreach (var boost in ownedBonuses)
        {
            if (boost.statType == statType)
            {
                total += boost.GetFlatBonusAtLevel(level);
                total += boost.GetPercentBonusAtLevel(level);
            }
        }

        return total;
    }

    /// <summary>
    /// Get total socketed bonus value for a specific stat type
    /// </summary>
    public float GetTotalSocketedBonus(StatType statType, int level = 1)
    {
        float total = 0f;

        if (socketedBonuses == null)
            return total;

        foreach (var boost in socketedBonuses)
        {
            if (boost.statType == statType)
            {
                total += boost.GetFlatBonusAtLevel(level);
                total += boost.GetPercentBonusAtLevel(level);
            }
        }

        return total;
    }

    /// <summary>
    /// Get element color for UI display
    /// </summary>
    public Color GetElementColor()
    {
        switch (element)
        {
            case WeaponElement.Fire:
                return new Color(1f, 0.3f, 0.2f); // Red-orange
            case WeaponElement.Ice:
                return new Color(0.3f, 0.7f, 1f); // Light blue
            case WeaponElement.Lightning:
                return new Color(1f, 1f, 0.3f); // Yellow
            case WeaponElement.Earth:
                return new Color(0.6f, 0.4f, 0.2f); // Brown
            case WeaponElement.Wind:
                return new Color(0.7f, 1f, 0.7f); // Light green
            case WeaponElement.Light:
                return new Color(1f, 1f, 0.9f); // Bright white
            case WeaponElement.Dark:
                return new Color(0.3f, 0.2f, 0.4f); // Purple-black
            case WeaponElement.Poison:
                return new Color(0.4f, 0.8f, 0.3f); // Toxic green
            case WeaponElement.Holy:
                return new Color(1f, 0.9f, 0.5f); // Golden
            default:
                return Color.white;
        }
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate item setup in editor
    /// </summary>
    private void OnValidate()
    {
        // Ensure itemId matches asset name if empty
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = name;
        }

        // Warn if socketable but no socketed bonuses
        if (canSocketToSoulWeapon && (socketedBonuses == null || socketedBonuses.Count == 0))
        {
            Debug.LogWarning($"{displayName}: Can socket but has no socketed bonuses!");
        }

        // Warn if has weapon skill but can't socket
        if (weaponSkill != null && !canSocketToSoulWeapon)
        {
            Debug.LogWarning($"{displayName}: Has weapon skill but can't be socketed!");
        }

        // Ensure display name is set
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = name;
        }

        // Check for balanced bonuses
        if (ownedBonuses != null && socketedBonuses != null)
        {
            float ownedTotal = 0f;
            float socketedTotal = 0f;

            foreach (var owned in ownedBonuses)
            {
                ownedTotal += owned.percentBonus;
            }

            foreach (var socketed in socketedBonuses)
            {
                socketedTotal += socketed.percentBonus;
            }

            // Warn if ratio is off (owned should be ~20% of socketed)
            if (ownedTotal > 0 && socketedTotal > 0)
            {
                float ratio = ownedTotal / socketedTotal;
                if (ratio > 0.3f)
                {
                    Debug.LogWarning($"{displayName}: Owned bonuses ({ownedTotal:F1}%) might be too strong compared to socketed ({socketedTotal:F1}%). Recommended ratio: ~20%");
                }
            }
        }
    }

    #endregion
}

#region Supporting Classes

/// <summary>
/// Single crafting requirement (resource + amount)
/// </summary>
[System.Serializable]
public class CraftingRequirement
{
    [Tooltip("Resource required for crafting")]
    public ResourceSO resource;

    [Tooltip("Amount of this resource needed")]
    public int amount;
}

/// <summary>
/// Individual stat boost (flat or percentage)
/// </summary>
[System.Serializable]
public class StatBoost
{
    [Tooltip("Which stat does this boost affect?")]
    public StatType statType;

    [Header("Flat Bonus")]
    [Tooltip("Flat addition (e.g., +10 Health)")]
    public float flatBonus;

    [Tooltip("How much the flat bonus increases per level")]
    public float flatBonusPerLevel;

    [Header("Percentage Bonus")]
    [Tooltip("Percentage boost (e.g., +15% Damage)")]
    public float percentBonus;

    [Tooltip("How much the percent bonus increases per level")]
    public float percentBonusPerLevel;

    /// <summary>
    /// Calculate the actual flat bonus for a given level
    /// </summary>
    public float GetFlatBonusAtLevel(int level)
    {
        return flatBonus + (flatBonusPerLevel * (level - 1));
    }

    /// <summary>
    /// Calculate the actual percent bonus for a given level
    /// </summary>
    public float GetPercentBonusAtLevel(int level)
    {
        return percentBonus + (percentBonusPerLevel * (level - 1));
    }
}

/// <summary>
/// Weapon element types for synergy system
/// </summary>
public enum WeaponElement
{
    None,
    Fire,
    Ice,
    Lightning,
    Earth,
    Wind,
    Light,
    Dark,
    Poison,
    Holy
}

/// <summary>
/// Item categories for filtering and organization
/// </summary>
public enum ItemCategory
{
    Weapon,
    Armor,
    Accessory,
    Consumable,
    Material,
    Special
}

#endregion

#region Example Usage in Comments

/*
 * ============================================
 * EXAMPLE: Setting up a Flame Blade weapon
 * ============================================
 * 
 * Basic Info:
 * - itemId: "FlameBlade"
 * - displayName: "Flame Blade"
 * - description: "A sword infused with fire magic"
 * - rarity: Epic
 * - category: Weapon
 * 
 * Crafting Requirements:
 * - FlameBladeFragment x 100
 * - SoulEssence x 5000
 * 
 * OWNED BONUSES (Always Active):
 * - Attack: +5% (flatBonus: 0, percentBonus: 5)
 * - Crit Damage: +2% (flatBonus: 0, percentBonus: 2)
 * 
 * SOCKETED BONUSES (When Socketed):
 * - Attack: +25% (flatBonus: 0, percentBonus: 25)
 * - Crit Damage: +15% (flatBonus: 0, percentBonus: 15)
 * 
 * Soul Weapon Integration:
 * - canSocketToSoulWeapon: true
 * - element: Fire
 * - weaponSkill: FlameBlade_Skill (Ability asset)
 * - weaponSkillCooldown: 10
 * - autoActivateSkill: true
 * 
 * Visual Feedback:
 * - collectionIcon: FlameBlade_Normal (normal sprite)
 * - socketedIcon: FlameBlade_Glowing (glowing sprite)
 * - skillActivationEffect: FlameSlash_Particles (particle system)
 * 
 * ============================================
 * PLAYER EXPERIENCE:
 * ============================================
 * 
 * When player CRAFTS Flame Blade:
 * - Gets +5% Attack, +2% Crit Damage immediately
 * - Can see it in collection
 * - Shows "Socket to unlock full power!" message
 * 
 * When player SOCKETS Flame Blade:
 * - Attack bonus increases: +5% → +30% (5% owned + 25% socketed)
 * - Crit Damage increases: +2% → +17% (2% owned + 15% socketed)
 * - Unlocks Flame Slash skill (activates every 10 seconds)
 * - Icon changes to glowing version
 * 
 * If paired with Ice weapon:
 * - Elemental Mastery synergy activates
 * - Bonus: +20% Attack from synergy
 * - Total Attack: +50% (30% from weapon + 20% synergy)
 * 
 */

#endregion