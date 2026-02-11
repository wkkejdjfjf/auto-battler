using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Manages character abilities, cooldowns, and activation
/// Handles both player and enemy ability systems
/// </summary>
public class Abilities : MonoBehaviour
{
    [Header("Ability Settings")]
    public List<Ability> abilities = new List<Ability>();
    public List<AbilityCooldown> cooldownUI;
    public List<Sprite> abilityImages;
    public CharacterStatsBase stats;
    public Ability basicAttack;
    public float attacksPerSecond = 1.0f;
    public Transform shootPoint;
    public float damageBuff = 0.0f;
    public bool isAuto = true;

    [Header("Enemy Settings")]
    [SerializeField] private float delaySeconds = 0.0f;

    [Header("Range Settings")]
    [SerializeField] private bool showRangeGizmos = false;
    [SerializeField] private Color rangeColor = Color.red;

    private bool isEnemy;
    private bool isActive = true;
    private Coroutine basicAttackCoroutine;
    private List<Coroutine> abilityCoroutines = new List<Coroutine>();
    public List<float> abilityCooldowns = new List<float>();
    public List<float> abilityCooldownTimes = new List<float>();
    private float _basicAttackCooldown = 1.0f;

    public delegate void AbilityActivated(Ability ability);
    public event AbilityActivated OnAbilityActivated;
    public event AbilityActivated OnFusedAbilityActivated;

    public delegate void AutoToggleChanged(bool isAuto);
    public static event AutoToggleChanged OnAutoToggleChanged;

    void Start()
    {
        Character character = gameObject.GetComponent<Character>();
        if (character != null)
        {
            isEnemy = character.isEnemy;
        }

        if (stats == null)
        {
            Debug.LogError("Stats component not assigned to Abilities script on " + gameObject.name);
            return;
        }

        if (!isEnemy)
        {
            SetupPlayerAbilities();
            CalculateAttackSpeed();
            StartBasicAttacks();
        }
        else
        {
            SetupEnemyAbilities();
            StartCoroutine(EnemyStartDelay());
        }
    }

    /// <summary>
    /// Setup player abilities from AbilityManager
    /// </summary>
    void SetupPlayerAbilities()
    {
        abilities.Clear();
        for (int i = 0; i < 6; i++)
        {
            abilities.Add(null);
        }

        AbilityManager abilityManager = FindFirstObjectByType<AbilityManager>();
        if (abilityManager != null)
        {
            for (int i = 0; i < 6; i++)
            {
                string equippedAbility = abilityManager.GetEquippedAbility(i);
                if (!string.IsNullOrEmpty(equippedAbility))
                {
                    Ability actualAbility = abilityManager.GetAbilityData(equippedAbility);
                    if (actualAbility != null)
                    {
                        abilities[i] = actualAbility;
                    }
                    else
                    {
                        Debug.LogError($"FAILED: Could not find ability data for '{equippedAbility}'");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("AbilityManager not found!");
        }

        abilityImages.Clear();
        abilityCooldowns.Clear();
        abilityCooldownTimes.Clear();
        for (int i = 0; i < 6; i++)
        {
            abilityCooldowns.Add(0.0f);
            abilityCooldownTimes.Add(0.0f);
        }

        if (cooldownUI != null)
        {
            for (int i = 0; i < cooldownUI.Count && i < 6; i++)
            {
                if (cooldownUI[i] != null)
                {
                    cooldownUI[i].gameObject.SetActive(false);
                }
            }
        }

        StopAbilityCoroutines();
    }

    /// <summary>
    /// Setup enemy abilities
    /// </summary>
    void SetupEnemyAbilities()
    {
        RefreshAbilityImages();
    }

    /// <summary>
    /// Refresh ability image list
    /// </summary>
    void RefreshAbilityImages()
    {
        abilityImages.Clear();
        foreach (var ability in abilities)
        {
            if (ability != null && ability.image != null)
            {
                abilityImages.Add(ability.image);
            }
        }
    }

    void OnEnable()
    {
        isActive = true;
        if (basicAttackCoroutine == null)
        {
            StartBasicAttacks();
        }
        if (!isEnemy)
        {
            StartAbilityCoroutines();
        }
    }

    void OnDisable()
    {
        isActive = false;
        StopBasicAttacks();
        StopAbilityCoroutines();
    }

    /// <summary>
    /// Toggle auto-cast on/off
    /// </summary>
    public void ToggleAuto(bool value)
    {
        isAuto = value;

        OnAutoToggleChanged?.Invoke(isAuto);

        if (!isEnemy)
        {
            if (isAuto)
            {
                StartAbilityCoroutines();
            }
            else
            {
                StopAbilityCoroutines();
            }
        }
    }

    /// <summary>
    /// Manually activate an ability (used when auto is off)
    /// </summary>
    public void ManuallyActivateAbility(int abilityIndex)
    {
        if (isAuto || isEnemy) return;

        if (!IsValidAbilityIndex(abilityIndex)) return;

        if (abilityCooldowns[abilityIndex] <= 0.0)
        {
            ActivateAbility(abilityIndex);
        }
    }

    /// <summary>
    /// Delay before enemy starts using abilities
    /// </summary>
    IEnumerator EnemyStartDelay()
    {
        yield return new WaitForSeconds((float)delaySeconds);
        CalculateAttackSpeed();
        StartBasicAttacks();
        InitializeCooldowns();
        StartAbilityCoroutines();
    }

    /// <summary>
    /// Initialize cooldown tracking for all abilities
    /// </summary>
    public void InitializeCooldowns()
    {
        abilityCooldowns.Clear();
        abilityCooldownTimes.Clear();

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != null)
            {
                abilityCooldowns.Add(0.0f);
                abilityCooldownTimes.Add(abilities[i].cooldownTime);

                if (!isEnemy && i < cooldownUI.Count && cooldownUI[i] != null)
                {
                    cooldownUI[i].SetMaxCooldown((float)abilities[i].cooldownTime);
                    cooldownUI[i].gameObject.SetActive(true);
                }
            }
            else
            {
                abilityCooldowns.Add(0.0f);
                abilityCooldownTimes.Add(0.0f);

                if (!isEnemy && i < cooldownUI.Count && cooldownUI[i] != null)
                {
                    cooldownUI[i].gameObject.SetActive(false);
                }
            }
        }

        RefreshAbilityImages();
    }

    /// <summary>
    /// Update cooldown timers each frame
    /// </summary>
    void Update()
    {
        if (!isActive) return;

        // Update ability cooldowns
        for (int i = 0; i < abilityCooldowns.Count; i++)
        {
            if (abilities[i] != null && abilityCooldowns[i] > 0)
            {
                abilityCooldowns[i] -= Time.deltaTime;
                UpdateCooldownUI(i);
            }
        }

        // Update basic attack cooldown
        if (_basicAttackCooldown > 0)
        {
            _basicAttackCooldown -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Start coroutines for auto-casting abilities
    /// </summary>
    void StartAbilityCoroutines()
    {
        StopAbilityCoroutines();

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != null)
            {
                int index = i;
                Coroutine routine = StartCoroutine(AutoActivateAbility(index));
                abilityCoroutines.Add(routine);
            }
        }
    }

    /// <summary>
    /// Stop all auto-cast coroutines
    /// </summary>
    void StopAbilityCoroutines()
    {
        foreach (var routine in abilityCoroutines)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        abilityCoroutines.Clear();
    }

    /// <summary>
    /// Auto-activate ability when conditions are met
    /// </summary>
    IEnumerator AutoActivateAbility(int abilityIndex)
    {
        while (isActive)
        {
            yield return new WaitUntil(() =>
                isAuto &&
                IsValidAbilityIndex(abilityIndex) &&
                abilityCooldowns[abilityIndex] <= 0.0 &&
                abilities[abilityIndex].CanActivate(gameObject)
            );

            if (!isActive || !isAuto) continue;

            if (IsValidAbilityIndex(abilityIndex))
            {
                ActivateAbility(abilityIndex);
            }

            yield return null;
        }
    }

    /// <summary>
    /// Check if ability index is valid
    /// </summary>
    bool IsValidAbilityIndex(int index)
    {
        return index >= 0 && index < abilities.Count && abilities[index] != null;
    }

    /// <summary>
    /// Activate an ability by index
    /// </summary>
    public void ActivateAbility(int abilityIndex)
    {
        if (!IsValidAbilityIndex(abilityIndex)) return;

        Ability ability = abilities[abilityIndex];
        if (!ability.CanActivate(gameObject)) return;

        OnAbilityActivated?.Invoke(ability);

        if (ability.typeOfAttack == Type.damage || ability.typeOfAttack == Type.PerEnemy)
        {
            ability.Activate(gameObject, damageBuff, stats.attack, stats.critDamage, stats.critChance);
        }
        else
        {
            ability.Activate(gameObject);
        }

        if (abilityIndex < abilityCooldowns.Count)
        {
            abilityCooldowns[abilityIndex] = abilityCooldownTimes[abilityIndex];

            if (!isEnemy && abilityIndex < cooldownUI.Count && cooldownUI[abilityIndex] != null)
            {
                cooldownUI[abilityIndex].SetCooldown((float)abilityCooldownTimes[abilityIndex]);
            }
        }
    }

    /// <summary>
    /// Activate a fusion ability (doesn't use cooldown system)
    /// </summary>
    public void ActivateFusionAbility(Ability fused)
    {
        OnFusedAbilityActivated?.Invoke(fused);

        if (fused.typeOfAttack == Type.damage || fused.typeOfAttack == Type.PerEnemy)
        {
            fused.Activate(gameObject, damageBuff, stats.attack, stats.critDamage, stats.critChance);
        }
        else
        {
            fused.Activate(gameObject);
        }
    }

    /// <summary>
    /// Start basic attack coroutine
    /// </summary>
    void StartBasicAttacks()
    {
        StopBasicAttacks();
        basicAttackCoroutine = StartCoroutine(BasicAttackRoutine());
    }

    /// <summary>
    /// Stop basic attack coroutine
    /// </summary>
    void StopBasicAttacks()
    {
        if (basicAttackCoroutine != null)
        {
            StopCoroutine(basicAttackCoroutine);
            basicAttackCoroutine = null;
        }
    }

    /// <summary>
    /// Basic attack loop
    /// </summary>
    IEnumerator BasicAttackRoutine()
    {
        while (isActive)
        {
            if (basicAttack == null)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            if (_basicAttackCooldown <= 0.0 && basicAttack.CanActivate(gameObject))
            {
                basicAttack.Activate(gameObject, damageBuff, stats.attack, stats.critDamage, stats.critChance);
                _basicAttackCooldown = 1.0f / attacksPerSecond;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Update cooldown UI display
    /// </summary>
    void UpdateCooldownUI(int abilityIndex)
    {
        if (isEnemy || abilityIndex >= cooldownUI.Count || cooldownUI[abilityIndex] == null) return;

        if (abilities[abilityIndex] != null)
        {
            cooldownUI[abilityIndex].SetCooldown((float)System.Math.Max(0.0, abilityCooldowns[abilityIndex]));
        }
    }

    /// <summary>
    /// Calculate and validate attack speed
    /// </summary>
    void CalculateAttackSpeed()
    {
        if (attacksPerSecond <= 0)
        {
            attacksPerSecond = 1.0f;
        }
    }

    /// <summary>
    /// Update attack speed value
    /// </summary>
    public void UpdateAttackSpeed(float newAttacksPerSecond)
    {
        attacksPerSecond = Mathf.Max(0.1f, newAttacksPerSecond);
    }

    /// <summary>
    /// Get current auto-cast state
    /// </summary>
    public bool GetAutoState()
    {
        return isAuto;
    }

    /// <summary>
    /// Called when abilities are changed (equipped/unequipped)
    /// </summary>
    public void OnAbilitiesChanged()
    {
        if (isEnemy) return;

        InitializeCooldowns();

        if (isAuto)
        {
            StartAbilityCoroutines();
        }

        StartCoroutine(DelayedUIUpdate());
    }

    /// <summary>
    /// Delayed UI update to ensure everything is synced
    /// </summary>
    private IEnumerator DelayedUIUpdate()
    {
        yield return new WaitForEndOfFrame();

        CooldownHandler cooldownHandler = FindFirstObjectByType<CooldownHandler>();
        if (cooldownHandler != null)
        {
            cooldownHandler.UpdateAbilityUI();
        }
    }

    /// <summary>
    /// Get count of equipped abilities
    /// </summary>
    public int GetEquippedAbilityCount()
    {
        return abilities.Count(a => a != null);
    }

    /// <summary>
    /// Check if ability exists in slot
    /// </summary>
    public bool HasAbilityInSlot(int slotIndex)
    {
        return IsValidAbilityIndex(slotIndex);
    }

    /// <summary>
    /// Draw ability ranges in editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showRangeGizmos) return;

        Gizmos.color = rangeColor;
        if (basicAttack != null)
        {
            Gizmos.DrawWireSphere(transform.position, (float)basicAttack.range);
        }

        foreach (var ability in abilities)
        {
            if (ability != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, (float)ability.range);
            }
        }
    }
}