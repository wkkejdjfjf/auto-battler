using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Level system that tracks player level, experience, and skill points
/// Handles experience curve calculations and persistent data
/// </summary>
public class LevelSystem : MonoBehaviour
{
    // PlayerPrefs keys
    private const string LEVEL_KEY = "LEVEL";
    private const string EXPERIENCE_KEY = "EXPERIENCE";
    private const string SKILL_POINTS_KEY = "SKILLPOINTS";

    public static LevelSystem Instance { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private double currentLevel = 1.0;
    [SerializeField] private double maxLevel = 100.0;
    [SerializeField] private double currentExp = 0.0;

    [Header("Experience Curve Settings")]
    [SerializeField] private double baseExpToLevelUp = 100.0; // Base XP needed for level 1 to 2
    [SerializeField] private double expGrowthFactor = 1.5;    // Exponential growth factor
    [SerializeField] private double additiveIncreasePerLevel = 50.0; // Additional flat increase per level

    [Header("Rewards")]
    [SerializeField] private int skillPointsPerLevel = 1;
    [SerializeField] private int currentSkillPoints = 0;

    [Header("Save Settings")]
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool autoSave = true;

    [Header("Events")]
    public UnityEvent<int> OnLevelUp; // Event triggered when level up occurs with new level as parameter
    public UnityEvent<double, double> OnExpGained; // Event with current and max exp

    // Dictionary to cache calculated exp requirements for each level
    private Dictionary<int, double> expRequirementCache = new Dictionary<int, double>();

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Initialize the exp requirement cache for faster lookup
        InitializeExpCache();

        // Load saved data if option is enabled
        if (loadOnStart)
        {
            LoadFromPlayerPrefs();
        }
    }

    private void Start()
    {
        // Initialize UI by triggering events
        InitializeUI();
    }

    /// <summary>
    /// Pre-calculate experience requirements for all levels
    /// </summary>
    private void InitializeExpCache()
    {
        // Make sure to start caching from level 1 (not 0)
        for (int i = 1; i <= (int)maxLevel; i++)
        {
            expRequirementCache[i] = CalculateExpForLevel(i);
        }
    }

    /// <summary>
    /// Initialize UI with current values
    /// </summary>
    private void InitializeUI()
    {
        // Trigger initial events to update UI
        OnLevelUp?.Invoke((int)currentLevel);

        // Make sure we're not at max level before trying to get next level exp requirement
        if (currentLevel < maxLevel)
        {
            OnExpGained?.Invoke(currentExp, expRequirementCache[(int)currentLevel]);
        }
        else
        {
            // Max level reached, send 0 as max exp
            OnExpGained?.Invoke(currentExp, 0);
        }
    }

    /// <summary>
    /// Calculates the exp required to level up from the specified level
    /// </summary>
    private double CalculateExpForLevel(int level)
    {
        if (level >= maxLevel) return double.MaxValue; // Cannot level up beyond max level

        // Exponential formula with additive component
        return baseExpToLevelUp * System.Math.Pow(expGrowthFactor, level - 1) + (additiveIncreasePerLevel * (level - 1));
    }

    /// <summary>
    /// Add experience points and handle level ups
    /// </summary>
    public void AddExperience(double expAmount)
    {
        // Don't add exp if already at max level
        if (currentLevel >= maxLevel)
            return;

        // Prevent overflow
        if (double.IsInfinity(currentExp + expAmount))
        {
            currentExp = double.MaxValue;
            Debug.LogWarning("Experience reached maximum value");
            return;
        }

        currentExp += expAmount;

        // Check if we can level up (possibly multiple times)
        bool leveledUp = false;
        while (currentLevel < maxLevel && currentExp >= expRequirementCache[(int)currentLevel])
        {
            // Subtract the required exp
            currentExp -= expRequirementCache[(int)currentLevel];

            // Level up
            currentLevel++;
            currentSkillPoints += skillPointsPerLevel;
            leveledUp = true;

            Debug.Log($"Level Up! Now level {currentLevel}. {currentSkillPoints} skill points available.");

            // Trigger the level up event
            OnLevelUp?.Invoke((int)currentLevel);
        }

        // Trigger the exp gained event
        double maxExp = (currentLevel < maxLevel) ? expRequirementCache[(int)currentLevel] : 0;
        OnExpGained?.Invoke(currentExp, maxExp);

        // If we reached max level, cap the exp
        if (currentLevel >= maxLevel)
        {
            currentExp = 0;
            Debug.Log("Maximum level reached!");
        }

        // Auto-save if enabled
        if (autoSave)
        {
            SaveToPlayerPrefs();
        }
    }

    /// <summary>
    /// Use skill points (called when player spends points on skills)
    /// </summary>
    public bool UseSkillPoints(int amount)
    {
        if (currentSkillPoints >= amount)
        {
            currentSkillPoints -= amount;
            Debug.Log($"Used {amount} skill points. {currentSkillPoints} remaining.");

            // Auto-save if enabled
            if (autoSave)
            {
                SaveToPlayerPrefs();
            }

            return true;
        }

        Debug.Log($"Not enough skill points. Have {currentSkillPoints}, need {amount}.");
        return false;
    }

    /// <summary>
    /// Save level data to PlayerPrefs
    /// </summary>
    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt(LEVEL_KEY, (int)currentLevel);

        // For large exp values, save as string
        if (currentExp > 1e30)
        {
            PlayerPrefs.SetString(EXPERIENCE_KEY + "_STR", currentExp.ToString());
        }
        else
        {
            PlayerPrefs.SetFloat(EXPERIENCE_KEY, (float)currentExp);
        }

        PlayerPrefs.SetInt(SKILL_POINTS_KEY, currentSkillPoints);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load level data from PlayerPrefs
    /// </summary>
    public void LoadFromPlayerPrefs()
    {
        if (PlayerPrefs.HasKey(LEVEL_KEY))
        {
            currentLevel = PlayerPrefs.GetInt(LEVEL_KEY, 1);
            // Ensure level is within valid range
            currentLevel = System.Math.Clamp(currentLevel, 1.0, maxLevel);
        }

        if (PlayerPrefs.HasKey(EXPERIENCE_KEY + "_STR"))
        {
            // Load from string for large values
            if (double.TryParse(PlayerPrefs.GetString(EXPERIENCE_KEY + "_STR"), out double loadedExp))
            {
                currentExp = loadedExp;
            }
        }
        else if (PlayerPrefs.HasKey(EXPERIENCE_KEY))
        {
            currentExp = PlayerPrefs.GetFloat(EXPERIENCE_KEY, 0f);
        }

        // Ensure we don't have more exp than needed for next level
        if (currentLevel < maxLevel)
        {
            currentExp = System.Math.Min(currentExp, expRequirementCache[(int)currentLevel] - 0.1);
        }

        if (PlayerPrefs.HasKey(SKILL_POINTS_KEY))
        {
            currentSkillPoints = PlayerPrefs.GetInt(SKILL_POINTS_KEY, 0);
        }

        Debug.Log($"Level data loaded: Level {currentLevel}, Exp {BigNumberFormatter.Format(currentExp, 0)}, Skill Points {currentSkillPoints}");
    }

    /// <summary>
    /// Clear saved data
    /// </summary>
    public void ClearSavedData()
    {
        PlayerPrefs.DeleteKey(LEVEL_KEY);
        PlayerPrefs.DeleteKey(EXPERIENCE_KEY);
        PlayerPrefs.DeleteKey(EXPERIENCE_KEY + "_STR");
        PlayerPrefs.DeleteKey(SKILL_POINTS_KEY);
        PlayerPrefs.Save();

        Debug.Log("Level data cleared from PlayerPrefs");
    }

    #region Getters

    /// <summary>
    /// Get current level
    /// </summary>
    public double GetCurrentLevel()
    {
        return currentLevel;
    }

    /// <summary>
    /// Get current experience
    /// </summary>
    public double GetCurrentExp()
    {
        return currentExp;
    }

    /// <summary>
    /// Get experience required for next level
    /// </summary>
    public double GetExpRequiredForNextLevel()
    {
        if (currentLevel >= maxLevel)
            return 0;

        return expRequirementCache[(int)currentLevel];
    }

    /// <summary>
    /// Get current skill points
    /// </summary>
    public int GetSkillPoints()
    {
        return currentSkillPoints;
    }

    /// <summary>
    /// Get the total experience gained so far (for statistics)
    /// </summary>
    public double GetTotalExpGained()
    {
        double total = currentExp;

        // Add exp for all completed levels
        for (int i = 1; i < (int)currentLevel; i++)
        {
            total += expRequirementCache[i];
        }

        return total;
    }

    /// <summary>
    /// Get experience progress as a percentage (0-1)
    /// </summary>
    public double GetLevelProgress()
    {
        if (currentLevel >= maxLevel)
            return 1.0;

        return currentExp / expRequirementCache[(int)currentLevel];
    }

    #endregion

    /// <summary>
    /// Reset the level system (for new game, etc.)
    /// </summary>
    public void ResetLevelSystem()
    {
        currentLevel = 1;
        currentExp = 0;
        currentSkillPoints = 0;

        // Trigger events to update UI
        OnLevelUp?.Invoke((int)currentLevel);

        if (currentLevel < maxLevel)
        {
            OnExpGained?.Invoke(currentExp, expRequirementCache[(int)currentLevel]);
        }
        else
        {
            OnExpGained?.Invoke(currentExp, 0);
        }

        // Auto-save if enabled
        if (autoSave)
        {
            SaveToPlayerPrefs();
        }
    }

    /// <summary>
    /// Called when the application is quitting
    /// </summary>
    private void OnApplicationQuit()
    {
        // Make sure data is saved when the game closes
        if (autoSave)
        {
            SaveToPlayerPrefs();
        }
    }

    /// <summary>
    /// Called when the game is paused/backgrounded on mobile
    /// </summary>
    private void OnApplicationPause(bool pause)
    {
        // Save when the game is paused
        if (pause && autoSave)
        {
            SaveToPlayerPrefs();
        }
    }
}