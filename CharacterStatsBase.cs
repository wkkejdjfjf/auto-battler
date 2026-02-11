using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Base stats component that all characters use
/// Handles combat calculations with double precision for idle game scaling
/// </summary>
public class CharacterStatsBase : MonoBehaviour
{
    [Header("Combat Stats")]
    public double maxHealth;
    public double currentHealth;
    public double attack;
    public double defense;
    public double shield;
    public float critChance = 5.0f;      // Stored as percentage (5 = 5%)
    public float critDamage = 1.5f;      // Multiplier (1.5 = 150% damage)

    [Header("Combat UI")]
    public GameObject healthBarParent;
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    public Slider shieldSlider;
    public TextMeshProUGUI shieldText;
    public Slider damageBufferSlider;

    [Header("Combat Settings")]
    public double damageBufferDelay = 0.5;
    public double damageBufferSpeed = 2.0;
    private double targetFillAmount;

    [Header("Combat Effects")]
    public DynamicTextData normalDamageText;
    public DynamicTextData healText;
    public DynamicTextData shieldTextData;

    // Character properties
    public bool isEnemy = false;
    public bool isAlive = true;
    protected bool isDead = false;

    // References to components
    protected Collider2D characterCollider;
    protected Rigidbody2D rb;
    protected AnimationHandler animator;
    protected WaveSystem waveSystem;

    // Events
    public event Action OnDeath;

    protected virtual void Awake()
    {
        // Get required components
        characterCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<AnimationHandler>();
    }

    protected virtual void Start()
    {
        // Find wave system
        waveSystem = FindFirstObjectByType<WaveSystem>();

        // Subscribe to wave events
        if (waveSystem != null)
        {
            waveSystem.OnWaveCompleted += OnWaveComplete;
        }

        // Initialize health
        currentHealth = maxHealth;

        // Initialize UI
        SetupUI();

        // Start UI update coroutine
        StartCoroutine(UpdateUI());
    }

    protected virtual void SetupUI()
    {
        if (healthBarParent != null)
            healthBarParent.SetActive(true);

        if (damageBufferSlider != null)
            damageBufferSlider.value = 1f;

        UpdateHealthUI();
        UpdateShieldUI();
    }

    #region Combat Methods

    /// <summary>
    /// Calculates damage output with attack scaling and critical hits
    /// </summary>
    public virtual double CalculateDamage(double baseDamage, bool forceCritical = false)
    {
        // Apply attack stat (every 100 attack = +100% damage)
        double damageAmount = baseDamage * (1.0 + (attack / 100.0));

        // Apply critical hit if needed
        bool isCrit = forceCritical || (UnityEngine.Random.value * 100.0 <= critChance);
        if (isCrit)
        {
            damageAmount *= critDamage;
        }

        return damageAmount;
    }

    /// <summary>
    /// Takes damage with defense calculation and shield absorption
    /// </summary>
    public virtual void TakeDamage(double amount, DynamicTextData customTextData = null)
    {
        // Apply defense reduction (diminishing returns formula)
        double damageReduction = defense / (defense + 100.0 * Math.Pow(amount, 0.5));
        double damageTaken = amount * (1.0 - damageReduction);

        // Ensure damage is valid
        if (double.IsNaN(damageTaken) || double.IsInfinity(damageTaken))
        {
            Debug.LogWarning($"Damage calculation resulted in invalid value. Original amount: {amount}");
            damageTaken = 0;
        }

        // Clamp to prevent negative damage
        damageTaken = Math.Max(0, damageTaken);

        // Create damage text using BigNumberFormatter
        DynamicTextData textData = customTextData != null ? customTextData : normalDamageText;
        DynamicTextManager.CreateText2D(
            transform.position,
            BigNumberFormatter.Format(damageTaken, 1),
            textData
        );

        // Apply damage to shield first, then health
        if (shield > 0)
        {
            if (damageTaken >= shield)
            {
                // Damage breaks through shield
                double remainingDamage = damageTaken - shield;
                shield = 0;
                currentHealth -= remainingDamage;
            }
            else
            {
                // Shield absorbs all damage
                shield -= damageTaken;
            }
            UpdateShieldUI();
        }
        else
        {
            // No shield, damage goes straight to health
            currentHealth -= damageTaken;
        }

        // Clamp health to valid range
        currentHealth = Math.Max(0, currentHealth);

        // Update health UI and buffer animation
        targetFillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0;
        UpdateHealthUI();
        StartCoroutine(AnimateDamageBuffer());

        // Check for death
        if (currentHealth <= 0 && !isDead)
        {
            isAlive = false;
            Die();
        }
    }

    /// <summary>
    /// Heals the character, overflow goes to shield
    /// </summary>
    public virtual void Heal(double amount)
    {
        if (currentHealth >= maxHealth)
        {
            // Already at max health, add to shield
            shield += amount;
            DynamicTextManager.CreateText2D(
                transform.position,
                BigNumberFormatter.Format(amount, 0),
                shieldTextData
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
                healText
            );

            // Add excess to shield
            double excessHeal = amount - healAmount;
            if (excessHeal > 0)
            {
                shield += excessHeal;
                DynamicTextManager.CreateText2D(
                    transform.position,
                    BigNumberFormatter.Format(excessHeal, 0),
                    shieldTextData
                );
            }
        }

        UpdateHealthUI();
        UpdateShieldUI();
    }

    /// <summary>
    /// Adds shield directly without healing
    /// </summary>
    public virtual void AddShield(double amount)
    {
        shield += amount;
        DynamicTextManager.CreateText2D(
            transform.position,
            BigNumberFormatter.Format(amount, 0),
            shieldTextData
        );
        UpdateShieldUI();
    }

    /// <summary>
    /// Handles character death
    /// </summary>
    protected virtual void Die()
    {
        isDead = true;
        StartCoroutine(DeathSequence());
    }

    /// <summary>
    /// Death animation and cleanup sequence
    /// </summary>
    protected virtual IEnumerator DeathSequence()
    {
        // Destroy collider and rigidbody
        if (characterCollider != null)
            Destroy(characterCollider);
        if (rb != null)
            Destroy(rb);

        // Hide UI
        if (healthBarParent != null)
            healthBarParent.SetActive(false);

        // Play death animation
        if (animator != null)
        {
            animator.DeathAnimation();
        }

        // Trigger death event
        OnDeath?.Invoke();

        // Wait before destroying object
        yield return new WaitForSeconds(1.2f);
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when a wave completes
    /// </summary>
    protected virtual void OnWaveComplete(int waveNum)
    {
        if (!isEnemy)
        {
            // Restore player health and reset shield between waves
            currentHealth = maxHealth;
            shield = 0;
            UpdateHealthUI();
            UpdateShieldUI();
        }
    }

    #endregion

    #region UI Methods

    /// <summary>
    /// Continuously updates UI elements
    /// </summary>
    protected virtual IEnumerator UpdateUI()
    {
        while (true)
        {
            UpdateHealthUI();
            UpdateShieldUI();
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// Updates health bar and text display
    /// </summary>
    protected virtual void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            // Update slider as a ratio (0-1)
            double healthRatio = maxHealth > 0 ? currentHealth / maxHealth : 0;
            healthSlider.value = (float)Math.Clamp(healthRatio, 0.0, 1.0);
        }

        if (healthText != null)
        {
            // Use BigNumberFormatter for clean display
            healthText.text = BigNumberFormatter.Format(currentHealth, 0);
        }
    }

    /// <summary>
    /// Updates shield bar and text display
    /// </summary>
    protected virtual void UpdateShieldUI()
    {
        if (shieldSlider != null)
        {
            // Shield shown as ratio of max health
            double shieldRatio = maxHealth > 0 ? shield / maxHealth : 0;
            shieldSlider.value = (float)Math.Clamp(shieldRatio, 0.0, 1.0);
        }

        if (shieldText != null)
        {
            // Use BigNumberFormatter for clean display
            shieldText.text = BigNumberFormatter.Format(shield, 0);
        }
    }

    /// <summary>
    /// Animates the damage buffer slider with delay
    /// </summary>
    protected virtual IEnumerator AnimateDamageBuffer()
    {
        if (damageBufferSlider == null)
            yield break;

        // Wait before starting animation
        yield return new WaitForSeconds((float)damageBufferDelay);

        // Gradually reduce buffer to target value
        while (damageBufferSlider.value > (float)targetFillAmount)
        {
            float currentValue = damageBufferSlider.value;
            float targetValue = (float)targetFillAmount;

            damageBufferSlider.value = Mathf.Lerp(
                currentValue,
                targetValue,
                Time.deltaTime * (float)damageBufferSpeed
            );

            // Stop when close enough
            if (Math.Abs(damageBufferSlider.value - targetValue) < 0.01f)
            {
                damageBufferSlider.value = targetValue;
                break;
            }

            yield return null;
        }
    }

    #endregion

    #region Unity Lifecycle

    protected virtual void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (waveSystem != null)
        {
            waveSystem.OnWaveCompleted -= OnWaveComplete;
        }
    }

    #endregion
}