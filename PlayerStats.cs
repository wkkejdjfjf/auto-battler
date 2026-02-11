using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Player-specific stats that inherit from CharacterStatsBase
/// Handles leveling, gold, equipment bonuses, and persistent data
/// </summary>
public class PlayerStats : CharacterStatsBase
{
    // Singleton instance
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    public double baseHealth = 100.0;
    public double baseAttack = 10.0;
    public double baseDefense = 5.0;
    public float baseCritChance = 5.0f;      // Percentage (5 = 5%)
    public float baseCritDamage = 1.5f;      // Multiplier

    [Header("Progress")]
    public double level = 1.0;
    public double gold = 0.0;

    // References
    private CraftingSystem craftingSystem;
    private UIManager uiManager;
    private LevelSystem levelSystem;
    private SoulWeapon soulWeapon;

    // Dictionary for storing stat boosts from items
    private Dictionary<StatType, double> statBoosts = new Dictionary<StatType, double>();

    // Events
    public delegate void StatsUpdatedHandler();
    public event StatsUpdatedHandler OnStatsUpdated;

    protected override void Awake()
    {
        base.Awake();

        // Setup singleton
        /*if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }*/

        isEnemy = false;

        // Initialize stat boosts dictionary
        InitializeStatBoosts();

        // Load player stats from PlayerPrefs
        LoadStatsFromPlayerPrefs();
    }

    protected override void Start()
    {
        // Find managers first so they're available during base.Start()
        uiManager = FindFirstObjectByType<UIManager>();
        craftingSystem = FindFirstObjectByType<CraftingSystem>();
        levelSystem = FindFirstObjectByType<LevelSystem>();
        soulWeapon = GetComponent<SoulWeapon>();

        // Subscribe to events
        if (craftingSystem != null)
        {
            craftingSystem.OnInventoryChanged += RecalculateStats;
        }

        // Calculate initial stats
        RecalculateStats();

        // Now call base Start
        base.Start();
    }

    /// <summary>
    /// Initialize all stat boost entries to zero
    /// </summary>
    private void InitializeStatBoosts()
    {
        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            statBoosts[statType] = 0.0;
        }
    }

    /// <summary>
    /// Load saved player stats from PlayerPrefs
    /// Note: PlayerPrefs only supports float, so we load as float and convert to double
    /// </summary>
    private void LoadStatsFromPlayerPrefs()
    {
        baseAttack = PlayerPrefs.GetFloat("BASE_ATK", (float)baseAttack);
        baseDefense = PlayerPrefs.GetFloat("BASE_DEF", (float)baseDefense);
        baseHealth = PlayerPrefs.GetFloat("BASE_HEALTH", (float)baseHealth);
        gold = PlayerPrefs.GetFloat("GOLD", 0f);
        level = PlayerPrefs.GetFloat("LEVEL", 1f);
    }

    /// <summary>
    /// Save current player stats to PlayerPrefs
    /// Note: For very large numbers (>10^38), consider using PlayerPrefs strings
    /// </summary>
    private void SaveStatsToPlayerPrefs()
    {
        // For numbers that might exceed float range, save as string
        if (baseAttack > 1e30 || baseDefense > 1e30 || baseHealth > 1e30 || gold > 1e30)
        {
            PlayerPrefs.SetString("BASE_ATK", baseAttack.ToString());
            PlayerPrefs.SetString("BASE_DEF", baseDefense.ToString());
            PlayerPrefs.SetString("BASE_HEALTH", baseHealth.ToString());
            PlayerPrefs.SetString("GOLD", gold.ToString());
        }
        else
        {
            // Safe to use float
            PlayerPrefs.SetFloat("BASE_ATK", (float)baseAttack);
            PlayerPrefs.SetFloat("BASE_DEF", (float)baseDefense);
            PlayerPrefs.SetFloat("BASE_HEALTH", (float)baseHealth);
            PlayerPrefs.SetFloat("GOLD", (float)gold);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Add to a base stat permanently
    /// </summary>
    public void AddBaseStat(StatType statType, double amount)
    {
        switch (statType)
        {
            case StatType.Attack:
                baseAttack += amount;
                break;
            case StatType.Defense:
                baseDefense += amount;
                break;
            case StatType.Health:
                baseHealth += amount;
                break;
        }

        RecalculateStats();
    }

    /// <summary>
    /// Recalculate all stats based on level, base stats, and equipment
    /// </summary>
    public void RecalculateStats()
    {
        // Update level if level system exists
        if (levelSystem != null)
        {
            level = levelSystem.GetCurrentLevel();
        }

        // Calculate base stats with level scaling
        attack = baseAttack * Math.Pow(level, 1.1);
        defense = baseDefense * Math.Pow(level, 1.1);
        maxHealth = baseHealth * Math.Pow(level, 1.1) * (1.0 + Math.Pow(baseDefense, 0.4)) * Math.Pow(baseAttack, 0.2);
        critChance = baseCritChance;
        critDamage = baseCritDamage;

        // Apply item boosts if crafting system exists
        ApplyItemBoosts();

        // Apply socketed weapon bonuses from Soul Weapon
        if (soulWeapon != null)
            soulWeapon.ApplySocketedBonusesToStats(this);

        // Initialize current health to max if it's not set yet
        if (currentHealth <= 0 || double.IsNaN(currentHealth))
        {
            currentHealth = maxHealth;
        }

        // Update UI
        UpdateHealthUI();
        UpdateShieldUI();

        // Notify listeners that stats have been updated
        OnStatsUpdated?.Invoke();

        // Save stats
        SaveStatsToPlayerPrefs();
        if (uiManager != null) uiManager.UpdateGoldUI();
    }

    /// <summary>
    /// Calculate and apply all item bonuses from equipped/crafted items
    /// </summary>
    private void ApplyItemBoosts()
    {
        if (craftingSystem == null) return;

        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            statBoosts[statType] = 0.0;
        }

        Dictionary<StatType, double> flatBoostTotals = new Dictionary<StatType, double>();
        Dictionary<StatType, double> percentBoostTotals = new Dictionary<StatType, double>();

        foreach (StatType statType in Enum.GetValues(typeof(StatType)))
        {
            flatBoostTotals[statType] = 0.0;
            percentBoostTotals[statType] = 0.0;
        }

        foreach (var itemPair in craftingSystem.craftedItemsData)
        {
            string itemId = itemPair.Key;
            ItemData itemData = itemPair.Value;

            CraftableItemSO craftableItem = craftingSystem.GetCraftableItemData(itemId);
            if (craftableItem == null) continue;

            foreach (var boost in craftableItem.ownedBonuses)
            {
                double flatBonus = boost.GetFlatBonusAtLevel(itemData.level);
                double percentBonus = boost.GetPercentBonusAtLevel(itemData.level) / 100.0;

                flatBoostTotals[boost.statType] += flatBonus;
                percentBoostTotals[boost.statType] += percentBonus;
                statBoosts[boost.statType] += flatBonus;
            }
        }

        ApplyStatBoost(StatType.Health, flatBoostTotals, percentBoostTotals, ref maxHealth);
        ApplyStatBoost(StatType.Attack, flatBoostTotals, percentBoostTotals, ref attack);
        ApplyStatBoost(StatType.Defense, flatBoostTotals, percentBoostTotals, ref defense);
        ApplyStatBoost(StatType.CritChance, flatBoostTotals, percentBoostTotals, ref critChance);
        ApplyStatBoost(StatType.CritDamage, flatBoostTotals, percentBoostTotals, ref critDamage);
    }

    /// <summary>
    /// Apply flat and percentage boosts to a specific stat
    /// </summary>
    private void ApplyStatBoost(StatType statType, Dictionary<StatType, double> flatBoosts,
                               Dictionary<StatType, double> percentBoosts, ref double statValue)
    {
        // Apply flat boost first
        statValue += flatBoosts[statType];

        // Then apply percentage boost
        statValue *= (1.0 + percentBoosts[statType]);

        // Track the percentage contribution for UI display
        statBoosts[statType] += GetBaseStat(statType) * percentBoosts[statType];
    }

    // Add overload for float stats (crit chance, crit damage)
    private void ApplyStatBoost(StatType statType, Dictionary<StatType, double> flatBoosts,
                               Dictionary<StatType, double> percentBoosts, ref float statValue)
    {
        // Apply flat boost first
        statValue += (float)flatBoosts[statType];

        // Then apply percentage boost
        statValue *= (float)(1.0 + percentBoosts[statType]);

        // Track the percentage contribution for UI display
        statBoosts[statType] += GetBaseStat(statType) * percentBoosts[statType];
    }

    /// <summary>
    /// Get the base value of a specific stat (before any boosts)
    /// </summary>
    private double GetBaseStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.Health: return baseHealth;
            case StatType.Attack: return baseAttack;
            case StatType.Defense: return baseDefense;
            case StatType.CritChance: return baseCritChance;
            case StatType.CritDamage: return baseCritDamage;
            default: return 0.0;
        }
    }

    #region Economy Methods

    /// <summary>
    /// Add gold to player's total
    /// </summary>
    public void AddGold(double amount)
    {
        gold += amount;
        if (uiManager != null) uiManager.UpdateGoldUI();
        SaveStatsToPlayerPrefs();
    }

    /// <summary>
    /// Remove gold from player's total
    /// </summary>
    public void RemoveGold(double amount)
    {
        gold -= amount;
        if (uiManager != null) uiManager.UpdateGoldUI();
        SaveStatsToPlayerPrefs();
    }

    /// <summary>
    /// Check if player can afford a purchase
    /// </summary>
    public bool CanAfford(double cost)
    {
        return gold >= cost;
    }

    #endregion

    #region Stat Getters

    /// <summary>
    /// Get the total boost applied to a stat from equipment
    /// </summary>
    public double GetStatBoost(StatType statType)
    {
        return statBoosts.ContainsKey(statType) ? statBoosts[statType] : 0.0;
    }

    /// <summary>
    /// Get the current value of a specific stat (including all boosts)
    /// </summary>
    public double GetStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.Health: return maxHealth;
            case StatType.Attack: return attack;
            case StatType.Defense: return defense;
            case StatType.CritChance: return critChance;
            case StatType.CritDamage: return critDamage;
            default: return 0.0;
        }
    }

    #endregion

    #region Reset Methods

    /// <summary>
    /// Resets all PlayerPrefs values to their default settings and recalculates stats
    /// </summary>
    public void ResetToDefaults()
    {
        // Reset base stats to their original values
        baseAttack = 10.0;
        baseDefense = 5.0;
        baseHealth = 100.0;
        level = 1.0;
        gold = 0.0;

        // Clear PlayerPrefs for this player's data
        PlayerPrefs.DeleteKey("BASE_ATK");
        PlayerPrefs.DeleteKey("BASE_DEF");
        PlayerPrefs.DeleteKey("BASE_HEALTH");
        PlayerPrefs.DeleteKey("GOLD");
        PlayerPrefs.DeleteKey("LEVEL");

        // Save the default values to PlayerPrefs
        SaveStatsToPlayerPrefs();

        // Reset current health to max health
        currentHealth = maxHealth;

        // Recalculate all stats with the reset values
        RecalculateStats();

        Debug.Log("Player stats reset to default values");
    }

    /// <summary>
    /// Alternative method to completely wipe ALL PlayerPrefs (use with caution!)
    /// </summary>
    public void ResetAllPlayerPrefs()
    {
        // This will delete ALL PlayerPrefs, not just player stats
        PlayerPrefs.DeleteAll();

        // Reload default values
        LoadStatsFromPlayerPrefs();

        // Recalculate stats
        RecalculateStats();

        Debug.Log("All PlayerPrefs have been reset");
    }

    #endregion
}