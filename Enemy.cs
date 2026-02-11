using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

/// <summary>
/// Enemy behavior including movement, death, and reward drops
/// Handles gold, experience, and resource drops on death
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Rewards")]
    public double gold;
    public double exp;
    public double dropMultiplier = 1.0;
    public EnemyResourceDrop[] resourceDrops;

    [Header("Movement")]
    [SerializeField] private double secondsTillStop = 2.0;
    [SerializeField] private double movementSpd = 1.0;
    public bool moving;

    [Header("Enemy Type")]
    public bool isBoss = false; // Set this in the prefab for boss enemies

    // References
    private PlayerStats stats;
    private Character chara;
    private CraftingUIManager cmanager;
    private WaveSystem waveSystem;

    void Start()
    {
        // Get component references
        chara = GetComponent<Character>();
        stats = FindFirstObjectByType<PlayerStats>();
        cmanager = FindFirstObjectByType<CraftingUIManager>();
        waveSystem = FindFirstObjectByType<WaveSystem>();

        // Subscribe to death event
        if (chara != null)
        {
            chara.death.AddListener(OnDeath);
        }

        // Start movement coroutine
        StartCoroutine(Movement());
    }

    void Update()
    {
        // Move the enemy to the right until it reaches the stop position
        if (moving && transform.position.x >= 1.5f)
        {
            transform.Translate(Vector2.right * (float)movementSpd * Time.deltaTime);
        }
    }

    /// <summary>
    /// Movement coroutine - enemy moves for a set duration then stops
    /// </summary>
    IEnumerator Movement()
    {
        moving = true;
        yield return new WaitForSeconds((float)secondsTillStop);
        moving = false;
    }

    /// <summary>
    /// Called when enemy dies - handles all rewards and cleanup
    /// </summary>
    public void OnDeath()
    {
        moving = false;

        // Give gold to player
        if (stats != null)
        {
            stats.AddGold(gold);
        }

        // Give experience to player
        LevelSystem levelSystem = FindFirstObjectByType<LevelSystem>();
        if (levelSystem != null)
        {
            levelSystem.AddExperience(exp);
        }

        // Drop fragments and resources
        if (cmanager != null)
        {
            // Drop rarity-based fragments
            DropFragmentsByRarity();

            // Keep old resource drops working
            DropOldResources();
        }
        else
        {
            Debug.LogWarning("CraftingUIManager is null - cannot drop resources");
        }

        // Notify wave system that this enemy is defeated
        if (waveSystem != null)
        {
            waveSystem.NotifyEnemyDeath(gameObject);
        }
    }

    /// <summary>
    /// Drop fragments based on wave and rarity tiers
    /// </summary>
    private void DropFragmentsByRarity()
    {
        if (waveSystem == null) return;

        int currentWave = waveSystem.GetCurrentWave();

        // Boss enemies get special drops
        if (isBoss)
        {
            DropBossFragments();
            return;
        }

        // Common Fragments (always available from Wave 1+)
        if (Random.Range(0f, 100f) < GetCommonDropChance(currentWave))
        {
            int amount = Random.Range(1, 4); // 1-3 fragments
            cmanager.AddResource("CommonFragment", amount);
        }

        // Rare Fragments (available from Wave 25+)
        if (currentWave >= 25 && Random.Range(0f, 100f) < GetRareDropChance(currentWave))
        {
            int amount = Random.Range(1, 3); // 1-2 fragments
            cmanager.AddResource("RareFragment", amount);
        }

        // Epic Fragments (available from Wave 50+)
        if (currentWave >= 50 && Random.Range(0f, 100f) < GetEpicDropChance(currentWave))
        {
            int amount = 1; // Always 1 fragment
            cmanager.AddResource("EpicFragment", amount);
        }

        // Legendary Fragments (available from Wave 100+)
        if (currentWave >= 100 && Random.Range(0f, 100f) < GetLegendaryDropChance(currentWave))
        {
            int amount = 1; // Always 1 fragment
            cmanager.AddResource("LegendaryFragment", amount);
        }
    }

    /// <summary>
    /// Keep old resource drops working (for materials like Iron Ore, etc.)
    /// </summary>
    private void DropOldResources()
    {
        foreach (var drop in resourceDrops)
        {
            if (drop.resourceSO != null)
            {
                if (Random.Range(1, 100) <= drop.percChance)
                {
                    int dropAmount = (int)System.Math.Round(dropMultiplier * drop.amount);
                    cmanager.AddResource(drop.resourceSO.resourceId, dropAmount);
                }
            }
        }
    }

    /// <summary>
    /// Special fragment drops for boss enemies
    /// </summary>
    private void DropBossFragments()
    {
        if (waveSystem == null) return;

        int currentWave = waveSystem.GetCurrentWave();

        // Bosses always drop fragments
        // Common: 20-40 fragments
        int commonAmount = Random.Range(20, 41);
        cmanager.AddResource("CommonFragment", commonAmount);

        // Rare: 10-20 fragments (if unlocked)
        if (currentWave >= 25)
        {
            int rareAmount = Random.Range(10, 21);
            cmanager.AddResource("RareFragment", rareAmount);
        }

        // Epic: 5-10 fragments (if unlocked)
        if (currentWave >= 50)
        {
            int epicAmount = Random.Range(5, 11);
            cmanager.AddResource("EpicFragment", epicAmount);
        }

        // Legendary: 1-3 fragments guaranteed (if unlocked)
        if (currentWave >= 100)
        {
            int legendaryAmount = Random.Range(1, 4);
            cmanager.AddResource("LegendaryFragment", legendaryAmount);

            Debug.Log($"Boss dropped {legendaryAmount} Legendary Fragments!");
        }
    }

    /// <summary>
    /// Calculate Common Fragment drop chance based on wave
    /// </summary>
    private float GetCommonDropChance(int wave)
    {
        if (wave < 25) return 5f;
        if (wave < 50) return 5f;
        if (wave < 100) return 4f;
        if (wave < 200) return 3f;
        return 2f;
    }

    /// <summary>
    /// Calculate Rare Fragment drop chance based on wave
    /// </summary>
    private float GetRareDropChance(int wave)
    {
        if (wave < 25) return 0f;
        if (wave < 50) return 2f;
        if (wave < 100) return 3f;
        if (wave < 200) return 3f;
        return 4f;
    }

    /// <summary>
    /// Calculate Epic Fragment drop chance based on wave
    /// </summary>
    private float GetEpicDropChance(int wave)
    {
        if (wave < 50) return 0f;
        if (wave < 100) return 0.5f;
        if (wave < 200) return 1f;
        if (wave < 500) return 1.5f;
        return 2f;
    }

    /// <summary>
    /// Calculate Legendary Fragment drop chance based on wave
    /// </summary>
    private float GetLegendaryDropChance(int wave)
    {
        if (wave < 100) return 0f;
        if (wave < 200) return 0.1f;
        if (wave < 500) return 0.3f;
        return 0.5f;
    }
}