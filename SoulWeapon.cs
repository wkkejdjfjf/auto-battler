using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Authentication;
using System.Net.Sockets;

public class SoulWeapon : MonoBehaviour
{
    [Header("Soul Weapon Stats")]
    [Tooltip("Main level of the Soul Weapon (increaes attack)")]
    public int weaponLevel = 1;

    [Tooltip("Current tier/rank of the weapon")]
    public WeaponTier currentTier = WeaponTier.Bronze;

    [Tooltip("Enhancement level within current tier (+0 to +25)")]
    public int enhancementLevel = 0;

    [Tooltip("Awakening stars for endgame progression")]
    [Range(0, 5)]
    public int awakeningStars = 0;


    [Header("Socket System")]
    [Tooltip("Maximum number of weapons that can be socketed")]
    public int maxSockets = 5;

    [Tooltip("Currently unlocked socket slots (increaes with Tier)")]
    public int unlockedSockets = 1;

    [Tooltip("Weapons currenlty socketed to the Soul Weapon")]
    public List<string> socketedWeaponIds = new List<string>(6);

    [Header("References")]
    private CraftingSystem craftingSystem;
    private PlayerStats playerStats;
    private Abilities abilitySystem;

    [Header("Skill Activation")]
    [Tooltip("Cooldowns for socketed weapon skills")]
    private Dictionary<string, float> weaponSkillCooldowns = new Dictionary<string, float>();

    public event Action<int> OnWeaponLevelUp;
    public event Action<WeaponTier> OnTierUp;
    public event Action<string> OnWeaponSocketed;
    public event Action<string> OnWeaponUnsocketd;

    private void Awake()
    {
        InitializeSockets();
        LoadSoulWeaponData();
    }

    private void Start()
    {
        craftingSystem = CraftingSystem.Instance;
        playerStats = GetComponent<PlayerStats>();
        abilitySystem = GetComponent<Abilities>();

        RecalculateBonuses();
    }

    private void Update()
    {
        UpdateWeaponSkills();
    }

    private void InitializeSockets()
    {
        while (socketedWeaponIds.Count < maxSockets)
        {
            socketedWeaponIds.Add("");
        }
    }

    #region Socketing System
    public bool SocketWeapon(string weaponId, int socketSlot)
    {
        if (socketSlot < 0 || socketSlot >= unlockedSockets)
        {
            Debug.LogWarning($"Socket slot {socketSlot} not available. Unlocked: {unlockedSockets}");
            return false;
        }

        if (craftingSystem == null || !craftingSystem.IsItemUnlocked(weaponId))
        {
            Debug.LogWarning($"Weapon {weaponId} not unlocked");
            return false;
        }

        for (int i = 0; i < socketedWeaponIds.Count; i++)
        {
            if (socketedWeaponIds[i] == weaponId && i != socketSlot)
            {
                Debug.LogWarning($"Weapon {weaponId} already socketed in another slot");
                return false;
            }
        }


        socketedWeaponIds[socketSlot] = weaponId;
        OnWeaponSocketed?.Invoke(weaponId);

        RecalculateBonuses();
        SaveSoulWeaponData();

        Debug.Log($"Socketed {weaponId} to slot {socketSlot}");
        return true;
    }

    public bool UnsocketWeapon(int socketSlot)
    {
        if (socketSlot < 0 || socketSlot >= socketedWeaponIds.Count)
            return false;

        string weaponId = socketedWeaponIds[socketSlot];
        if (string.IsNullOrEmpty(weaponId))
            return false;

        socketedWeaponIds[socketSlot] = "";
        OnWeaponUnsocketd?.Invoke(weaponId);

        RecalculateBonuses();
        SaveSoulWeaponData();

        Debug.Log($"Unsocketed {weaponId} from slot {socketSlot}");
        return true;
    }

    public string GetSocketedWeapon(int socketSlot)
    {
        if (socketSlot < 0 || socketSlot >= socketedWeaponIds.Count)
            return "";
        return socketedWeaponIds[socketSlot];
    }

    public bool IsSocketEmpty(int socketSlot)
    {
        if (socketSlot < 0 || socketSlot >= socketedWeaponIds.Count)
            return true;
        return string.IsNullOrEmpty(socketedWeaponIds[socketSlot]);
    }

    #endregion

    #region Leveling & Upgrading

    public bool LevelUpWeapon(int expAmount)
    {
        // Use Soul Essence as leveling currency

        if (craftingSystem == null)
            return false;

        int essenceCost = GetLevelUpCost();
        if (craftingSystem.GetResourceAmount("SoulEssence") < essenceCost)
            return false;

        craftingSystem.RemoveResource("SoulEssence", essenceCost);
        weaponLevel++;

        OnWeaponLevelUp?.Invoke(weaponLevel);
        RecalculateBonuses();
        SaveSoulWeaponData();

        Debug.Log($"Leveled up Soul Weapon to level {weaponLevel}");
        return true;
    }

    public int GetLevelUpCost()
    {
        return 100 + (weaponLevel * 50);
    }

    public bool EnhanceWeapon()
    {
        if (enhancementLevel >= 25)
        {
            Debug.Log("Max enhancement level reached for current tier");
            return false;
        }

        if (craftingSystem == null)
            return false;

        int stoneCost = (enhancementLevel + 1) * 10;
        if (craftingSystem.GetResourceAmount("EnhancementStone") < stoneCost)
            return false;

        craftingSystem.RemoveResource("EnhancementStone", stoneCost);
        enhancementLevel++;

        RecalculateBonuses();
        SaveSoulWeaponData();

        Debug.Log($"Soul Weapon enhanced to +{enhancementLevel}");
        return true;
    }

    public bool UpgradeTier()
    {
        if (currentTier >= WeaponTier.Divine)
        {
            Debug.Log("Already maximum Tier");
            return false;
        }

        int requiredLevel = GetTierMaxLevel();
        if (weaponLevel < requiredLevel)
        {
            Debug.Log($"Need to reach level {requiredLevel} to upgrade Tier");
            return false;
        }

        if (enhancementLevel < 25)
        {
            Debug.Log("Need to reach +25 enhancement to upgrade Tier");
        }

        if (!HasTierUpgradeMaterials())
            return false;

        ConsumeTierUpgradeMaterials();
        currentTier++;
        enhancementLevel = 0;

        UnlockSocketsForTier();

        OnTierUp?.Invoke(currentTier);
        RecalculateBonuses();
        SaveSoulWeaponData();

        Debug.Log($"Upgraded Soul Weapon to {currentTier} Tier");
        return true;
    }

    private int GetTierMaxLevel()
    {
        return (int)currentTier * 100 + 100;
    }

    private bool HasTierUpgradeMaterials()
    {
        if (craftingSystem == null)
            return false;

        string coreName = GetRequiredCoreForTier();
        int coreAmount = GetRequiredCoreAmount();

        return craftingSystem.GetResourceAmount(coreName) >= coreAmount;
    }

    private void ConsumeTierUpgradeMaterials()
    {
        string coreName = GetRequiredCoreForTier();
        int coreAmount = GetRequiredCoreAmount();

        craftingSystem.RemoveResource(coreName, coreAmount);
    }

    private string GetRequiredCoreForTier()
    {
        switch (currentTier)
        {
            case WeaponTier.Bronze: return "Silvercore";
            case WeaponTier.Silver: return "Goldcore";
            case WeaponTier.Gold: return "Platinumcore";
            case WeaponTier.Platinum: return "Divinecore";
            default: return "SilverCore";
        }
    }

    private int GetRequiredCoreAmount()
    {
        return ((int)currentTier + 1) * 5;
    }

    private void UnlockSocketsForTier()
    {
        int socketsForTier = Mathf.Min(1 + (int)currentTier, maxSockets);
        unlockedSockets = socketsForTier;
    }

    #endregion

    #region Stat Calculation

    private double cachedAttackMultiplier = 1.0;
    private double cachedHealthMultiplier = 1.0;
    private double cachedDefenseMultiplier = 1.0;
    private float cachedCritDamageBonus = 0f;
    private float cachedCritChanceBonus = 0f;

    public void RecalculateBonuses()
    {
        if (craftingSystem == null)
            return;

        cachedAttackMultiplier = 1.0;
        cachedHealthMultiplier = 1.0;
        cachedDefenseMultiplier = 1.0;
        cachedCritDamageBonus = 0f;
        cachedCritChanceBonus = 0f;

        foreach (string weaponId in socketedWeaponIds)
        {
            if (string.IsNullOrEmpty(weaponId))
                continue;

            CraftableItemSO weapon = craftingSystem.GetCraftableItemData(weaponId);
            if (weapon == null)
                continue;

            int weaponLevel = craftingSystem.GetItemLevel(weaponId);

            foreach (var boost in weapon.socketedBonuses)
            {
                float flatBonus = boost.GetFlatBonusAtLevel(weaponLevel);
                float percentBonus = boost.GetPercentBonusAtLevel(weaponLevel);

                switch (boost.statType)
                {
                    case StatType.Attack:
                        cachedAttackMultiplier += percentBonus / 100.0;
                        break;
                    case StatType.CritDamage:
                        cachedCritDamageBonus += percentBonus;
                        break;
                    case StatType.CritChance:
                        cachedCritChanceBonus += percentBonus;
                        break;
                    case StatType.Defense:
                        cachedDefenseMultiplier += percentBonus / 100.0;
                        break;
                    case StatType.Health:
                        cachedHealthMultiplier += percentBonus / 100.0;
                        break;
                }
            }
        }

        ApplySynergyBonuses(ref cachedAttackMultiplier, ref cachedCritDamageBonus);

        if (playerStats != null)
            playerStats.RecalculateStats();

        Debug.Log($"Soul Weapon cached bonuses recalculated. Attack x{cachedAttackMultiplier:F2}");
    }

    public void ApplySocketedBonusesToStats(PlayerStats stats)
    {
        stats.attack *= cachedAttackMultiplier;
        stats.maxHealth *= cachedHealthMultiplier;
        stats.defense *= cachedDefenseMultiplier;
        stats.critDamage += cachedCritDamageBonus;
        stats.critChance += cachedCritChanceBonus;
    }

    private double CalculateSoulWeaponAttack()
    {
        // Base attack from level
        double baseAttack = weaponLevel * 100.0;

        // Tier multiplier
        double tierMultiplier = 1.0 + ((int)currentTier * 0.5);

        // Enhancement bonus (each + gives 2% attack)
        double enhancementBonus = 1.0 + (enhancementLevel * 0.02);

        // Awakening bonus (endgame)
        double awakeningBonus = 1.0 + (awakeningStars * 0.25);

        return baseAttack * tierMultiplier * enhancementBonus * awakeningBonus;
    }

    private void ApplySynergyBonuses(ref double attackBonus, ref float critDamageBonus)
    {
        bool hasFireWeapon = false;
        bool hasIceWeapon = false;

        foreach (string weaponId in socketedWeaponIds)
        {
            if (string.IsNullOrEmpty(weaponId))
                continue;

            if (weaponId.Contains("Fire") || weaponId.Contains("Flame"))
                hasFireWeapon = true;
            if (weaponId.Contains("Ice") || weaponId.Contains("Frost"))
                hasIceWeapon = true;
        }

        if (hasFireWeapon && hasIceWeapon)
        {
            attackBonus += 0.20;
            Debug.Log("Elemental Mastery synergy activated! +20% attack");
        }
    }

    public double GetTotalAttack()
    {
        return CalculateSoulWeaponAttack();
    }

    #endregion

    #region Weapon Skills

    /// <summary>
    /// Update and activate socketed weapon skills
    /// </summary>
    private void UpdateWeaponSkills()
    {
        if (abilitySystem == null || craftingSystem == null)
            return;

        foreach (string weaponId in socketedWeaponIds)
        {
            if (string.IsNullOrEmpty(weaponId))
                continue;

            // Check cooldown
            if (weaponSkillCooldowns.ContainsKey(weaponId))
            {
                weaponSkillCooldowns[weaponId] -= Time.deltaTime;
                if (weaponSkillCooldowns[weaponId] > 0)
                    continue;
            }

            // Try to activate weapon skill
            CraftableItemSO weapon = craftingSystem.GetCraftableItemData(weaponId);
            if (weapon == null)
                continue;

            // Get the weapon's ability (you'll need to add this to CraftableItemSO)
            Ability weaponSkill = GetWeaponSkill(weapon);
            if (weaponSkill == null)
                continue;

            // Check if skill can activate
            if (weaponSkill.CanActivate(gameObject))
            {
                // Activate the skill
                if (weaponSkill.typeOfAttack == Type.damage || weaponSkill.typeOfAttack == Type.PerEnemy)
                {
                    weaponSkill.Activate(gameObject, 0, playerStats.attack, playerStats.critDamage, playerStats.critChance);
                }
                else
                {
                    weaponSkill.Activate(gameObject);
                }

                // Set cooldown
                weaponSkillCooldowns[weaponId] = weaponSkill.cooldownTime;

                Debug.Log($"Activated weapon skill from {weaponId}");
            }
        }
    }

    private Ability GetWeaponSkill(CraftableItemSO weapon)
    {
        // You'll need to add an Ability field to CraftableItemSO
        // For now, return null - we'll add this in the next step
        return null;
    }

    #endregion

    #region Save/Load

    private void LoadSoulWeaponData()
    {
        weaponLevel = PlayerPrefs.GetInt("SoulWeapon_Level", 1);
        currentTier = (WeaponTier)PlayerPrefs.GetInt("SoulWeapon_Tier", 0);
        enhancementLevel = PlayerPrefs.GetInt("SoulWeapon_Enhancement", 0);
        awakeningStars = PlayerPrefs.GetInt("SoulWeapon_Awakening", 0);
        unlockedSockets = PlayerPrefs.GetInt("SoulWeapon_UnlockedSockets", 2);

        // Load socketed weapons
        string socketData = PlayerPrefs.GetString("SoulWeapon_Sockets", "");
        if (!string.IsNullOrEmpty(socketData))
        {
            string[] sockets = socketData.Split('|');
            for (int i = 0; i < sockets.Length && i < maxSockets; i++)
            {
                socketedWeaponIds[i] = sockets[i];
            }
        }

        Debug.Log($"Loaded Soul Weapon: Level {weaponLevel}, Tier {currentTier}");
    }

    private void SaveSoulWeaponData()
    {
        PlayerPrefs.SetInt("SoulWeapon_Level", weaponLevel);
        PlayerPrefs.SetInt("SoulWeapon_Tier", (int)currentTier);
        PlayerPrefs.SetInt("SoulWeapon_Enhancement", enhancementLevel);
        PlayerPrefs.SetInt("SoulWeapon_Awakening", awakeningStars);
        PlayerPrefs.SetInt("SoulWeapon_UnlockedSockets", unlockedSockets);

        // Save socketed weapons
        string socketData = string.Join("|", socketedWeaponIds);
        PlayerPrefs.SetString("SoulWeapon_Sockets", socketData);

        PlayerPrefs.Save();
    }

    #endregion

    #region Public Getters

    public int GetWeaponLevel() => weaponLevel;
    public WeaponTier GetCurrentTier() => currentTier;
    public int GetEnhancementLevel() => enhancementLevel;
    public int GetAwakeningStars() => awakeningStars;
    public int GetUnlockedSockets() => unlockedSockets;
    public int GetMaxSockets() => maxSockets;

    #endregion
}

public enum WeaponTier
{
    Bronze = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3,
    Divine = 4
}

public enum WeaponFragment
{
    FlameSwordFragment,
    FlameSpearFragment
}

public enum CommonResource
{
    IronOre,
    SteelIngot,
    MithrilShard,

    SoulEssence,

    // Enhancement materials
    WhetstoneFragment,
    EnhancementCrystal
}