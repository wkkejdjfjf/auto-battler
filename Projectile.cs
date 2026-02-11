using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Projectile that travels in a direction and deals damage on hit
/// Supports critical hits and explosion damage
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed;

    [Header("Damage Stats")]
    public double power;
    private double damage;
    public double buff;

    [HideInInspector]
    public double characterAtk;
    [HideInInspector]
    public double characterCritMulti;
    [HideInInspector]
    public double characterCritPerc;

    [Header("Explosion")]
    public GameObject explosion;
    public Transform explosionPosition;
    public double explosionRadius;
    public double explosionDamage;

    [Header("References")]
    public GameObject shooter;
    public DynamicTextData critData;

    private bool hasExploded;
    private bool isCrit;

    private void Start()
    {
        damage = 0;

        // Calculate if this is a critical hit
        if (UnityEngine.Random.value <= (characterCritPerc / 100.0))
        {
            isCrit = true;
            damage = (power / 100.0) * characterCritMulti * (1.0 + (buff / 100.0)) * characterAtk;
            explosionDamage = (explosionDamage / 100.0) * characterAtk * characterCritMulti * (1.0 + (buff / 100.0));
        }
        else
        {
            damage = (power / 100.0) * (1.0 + (buff / 100.0)) * characterAtk;
            explosionDamage = (explosionDamage / 100.0) * characterAtk * (1.0 + (buff / 100.0));
        }

        // Add null check for explosion GameObject
        if (explosion != null && explosion.GetComponent<ExplosionLeader>() != null)
        {
            explosion.GetComponent<ExplosionLeader>().projectile = this;
        }
    }

    public void FixedUpdate()
    {
        gameObject.transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    /// <summary>
    /// Handle collision with targets
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Null checks and validation
        if (collision == null || collision.gameObject == null)
        {
            Debug.LogWarning("Projectile hit null collision object");
            return;
        }

        // Check for shooter
        if (shooter == null)
        {
            Debug.LogWarning("Projectile has no shooter assigned");
            return;
        }

        // Check if already exploded to prevent multiple explosions
        if (hasExploded)
        {
            return;
        }

        // Use CompareTag instead of direct tag comparison (more efficient and safer)
        if (!collision.gameObject.CompareTag(shooter.tag))
        {
            // Check if the target has CharacterStatsBase component before trying to damage it
            CharacterStatsBase targetStats = collision.gameObject.GetComponent<CharacterStatsBase>();

            if (targetStats != null)
            {
                // Apply direct hit damage
                if (isCrit)
                {
                    targetStats.TakeDamage(damage, critData);
                }
                else
                {
                    targetStats.TakeDamage(damage);
                }
            }
            else
            {
                // Log for debugging - the projectile hit something without CharacterStatsBase
                Debug.Log($"Projectile hit {collision.gameObject.name} which has no CharacterStatsBase component");
            }

            // Add null checks for explosion instantiation
            if (explosion != null && explosionPosition != null)
            {
                Instantiate(explosion, explosionPosition.position, transform.rotation);
            }
            else if (explosion != null)
            {
                // Fallback to projectile position if explosionPosition is null
                Instantiate(explosion, transform.position, transform.rotation);
            }

            // Trigger explosion
            Explode();
        }
    }

    /// <summary>
    /// Handle explosion damage to all targets in radius
    /// </summary>
    private void Explode()
    {
        // Prevent multiple explosions
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;

        // Find all enemies in explosion radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, (float)explosionRadius);

        // Apply explosion damage to all enemies in radius
        foreach (Collider2D hitCollider in hitColliders)
        {
            // Added null checks for each collider
            if (hitCollider == null || hitCollider.gameObject == null)
            {
                continue;
            }

            CharacterStatsBase enemy = hitCollider.gameObject.GetComponent<CharacterStatsBase>();

            // Added null check for shooter and proper tag comparison
            if (enemy != null && shooter != null && !hitCollider.gameObject.CompareTag(shooter.tag))
            {
                // Apply falloff based on distance
                float distance = Vector2.Distance(transform.position, hitCollider.transform.position);
                double damageMultiplier = System.Math.Clamp(1.0 - (distance / explosionRadius), 0.0, 1.0);
                double finalDamage = explosionDamage * damageMultiplier;

                // Only apply damage if it's greater than 0
                if (finalDamage > 0)
                {
                    enemy.TakeDamage(finalDamage);
                }
            }
        }

        // Destroy the projectile
        Destroy(gameObject);
    }

    /// <summary>
    /// Visualize explosion radius in editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, (float)explosionRadius);
    }

    /// <summary>
    /// Validation method to check if projectile is properly set up
    /// </summary>
    public bool IsValidProjectile()
    {
        if (shooter == null)
        {
            Debug.LogError("Projectile shooter is null!");
            return false;
        }

        if (characterAtk <= 0)
        {
            Debug.LogWarning("Projectile characterAtk is 0 or negative!");
        }

        return true;
    }
}