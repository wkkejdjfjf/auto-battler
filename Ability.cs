using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base ScriptableObject for all abilities in the game
/// Handles activation, targeting, range checks, and fusion
/// </summary>
public class Ability : ScriptableObject
{
    public new string name;
    public Type typeOfAttack;
    public float cooldownTime;
    public Sprite image;
    public Rarity rarity;

    [Header("Range Settings")]
    public float range = 5.0f; // Range in units
    public bool requiresTarget = true; // Some abilities might not need targets (like buffs/heals)
    public LayerMask targetLayers = -1; // What layers to check for targets

    [Header("Ability Fusion")]
    public Ability fusedWith;
    public Ability fusionResult;

    /// <summary>
    /// Basic activation (override in derived classes)
    /// </summary>
    public virtual void Activate()
    {
    }

    /// <summary>
    /// Activation with parent reference
    /// </summary>
    public virtual void Activate(GameObject parent)
    {
    }

    /// <summary>
    /// Activation with full combat stats (for damage abilities)
    /// </summary>
    public virtual void Activate(GameObject parent, float buff, double atkstat, float critDamage, float critChance)
    {
    }

    /// <summary>
    /// Check if ability can be activated based on range and targets
    /// </summary>
    public virtual bool CanActivate(GameObject caster)
    {
        if (!requiresTarget) return true;
        return HasTargetsInRange(caster);
    }

    /// <summary>
    /// Check if there are valid targets in range
    /// </summary>
    protected bool HasTargetsInRange(GameObject caster)
    {
        // Get the character component to determine if we're looking for enemies or allies
        Character character = caster.GetComponent<Character>();
        if (character == null) return false;

        // Find all potential targets in range
        Collider2D[] targetsInRange = Physics2D.OverlapCircleAll(
            caster.transform.position,
            (float)range,
            targetLayers
        );

        foreach (Collider2D target in targetsInRange)
        {
            Character targetCharacter = target.GetComponent<Character>();
            if (targetCharacter == null) continue;

            // Skip self
            if (target.gameObject == caster) continue;

            // Check if target is valid based on ability type and caster
            if (IsValidTarget(character, targetCharacter))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a specific target is valid for this ability
    /// </summary>
    protected virtual bool IsValidTarget(Character caster, Character target)
    {
        // For damage abilities, target enemies
        if (typeOfAttack == Type.damage || typeOfAttack == Type.debuff || typeOfAttack == Type.PerEnemy)
        {
            return caster.isEnemy != target.isEnemy; // Different teams
        }

        // For buffs and heals, target allies (including self)
        if (typeOfAttack == Type.buff || typeOfAttack == Type.heal)
        {
            return caster.isEnemy == target.isEnemy; // Same team
        }

        return false;
    }

    /// <summary>
    /// Get the closest valid target within range
    /// </summary>
    public GameObject GetClosestTargetInRange(GameObject caster)
    {
        Character character = caster.GetComponent<Character>();
        if (character == null) return null;

        Collider2D[] targetsInRange = Physics2D.OverlapCircleAll(
            caster.transform.position,
            (float)range,
            targetLayers
        );

        GameObject closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D target in targetsInRange)
        {
            Character targetCharacter = target.GetComponent<Character>();
            if (targetCharacter == null) continue;

            if (target.gameObject == caster) continue;

            if (IsValidTarget(character, targetCharacter))
            {
                float distance = Vector2.Distance(caster.transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target.gameObject;
                }
            }
        }

        return closestTarget;
    }
}

/// <summary>
/// Types of abilities available in the game
/// </summary>
public enum Type
{
    damage,    // Direct damage to enemies
    debuff,    // Negative effects on enemies
    buff,      // Positive effects on allies
    heal,      // Restore health to allies
    PerEnemy,  // Damage that scales per enemy
    AOE,       // Area of effect
    Shield     // Add shield/temporary HP
}