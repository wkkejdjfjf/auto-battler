using System;
using UnityEngine;

/// <summary>
/// Enemy-specific stats that inherit from CharacterStatsBase
/// Handles enemy scaling and custom text data
/// </summary>
public class EnemyStats : CharacterStatsBase
{
    [Header("Enemy Setup")]
    public double enemyLevel = 1.0;
    public double baseAttackMultiplier = 1.0;
    public double baseDefenseMultiplier = 1.0;
    public double baseHealthMultiplier = 1.0;

    [Header("Enemy Text Data")]
    public DynamicTextData normData;
    public DynamicTextData healData;
    public DynamicTextData shieldData;

    protected override void Awake()
    {
        base.Awake();
        isEnemy = true;
    }

    protected override void Start()
    {
        // Calculate enemy stats based on level (if using dynamic scaling)
        // CalculateEnemyStats();

        // Call base Start after calculating stats
        base.Start();
    }

    /// <summary>
    /// Calculate enemy stats based on level and multipliers
    /// Uncomment and use this if you want dynamic enemy scaling
    /// </summary>
    private void CalculateEnemyStats()
    {
        // Exponential level-based scaling for enemies
        maxHealth = baseHealthMultiplier * 100.0 * Math.Pow(enemyLevel, 1.1);
        attack = baseAttackMultiplier * 10.0 * Math.Pow(enemyLevel, 1.2);
        defense = baseDefenseMultiplier * 5.0 * Math.Pow(enemyLevel, 1.1);

        // Initialize health
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Sets the enemy's level and recalculates stats
    /// </summary>
    public void SetEnemyLevel(double level)
    {
        enemyLevel = level;
        // CalculateEnemyStats();
        UpdateHealthUI();
        UpdateShieldUI();
    }

    /// <summary>
    /// Override to use enemy-specific damage text data
    /// </summary>
    public override void TakeDamage(double amount, DynamicTextData customTextData = null)
    {
        // Use enemy-specific text data if no custom data is provided
        DynamicTextData textToUse = customTextData != null ? customTextData : normData;
        base.TakeDamage(amount, textToUse);
    }

    /// <summary>
    /// Override to use enemy-specific heal text data with BigNumberFormatter
    /// </summary>
    public override void Heal(double amount)
    {
        if (currentHealth >= maxHealth)
        {
            // Already at max health, add to shield
            shield += amount;
            DynamicTextManager.CreateText2D(
                transform.position,
                BigNumberFormatter.Format(amount, 0),
                shieldData
            );
        }
        else
        {
            // Calculate how much to heal and how much goes to shield
            double healAmount = Math.Min(amount, maxHealth - currentHealth);
            currentHealth += healAmount;

            // Display heal text
            DynamicTextManager.CreateText2D(
                transform.position,
                BigNumberFormatter.Format(healAmount, 0),
                healData
            );

            // Add excess to shield
            double excessHeal = amount - healAmount;
            if (excessHeal > 0)
            {
                shield += excessHeal;
                DynamicTextManager.CreateText2D(
                    transform.position,
                    BigNumberFormatter.Format(excessHeal, 0),
                    shieldData
                );
            }
        }

        UpdateHealthUI();
        UpdateShieldUI();
    }

    /// <summary>
    /// Override to prevent enemies from healing between waves
    /// </summary>
    protected override void OnWaveComplete(int waveNum)
    {
        // Enemies don't restore health between waves
        // Override to prevent base behavior
    }
}