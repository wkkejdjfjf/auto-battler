using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Multi-slash ability that performs a series of attacks on enemies
/// Includes initial slash, progressive slashes, and final slash with visual effects
/// </summary>
public class MultiSlash : MonoBehaviour
{
    [Header("Ability Stats")]
    public double power;
    public double buff;
    public double range;
    public GameObject shooter;

    [HideInInspector]
    public double characterAtk;
    [HideInInspector]
    public double characterCritMulti;
    [HideInInspector]
    public double characterCritPerc;

    [Header("Damage Text")]
    public DynamicTextData normData;
    public DynamicTextData critData;
    private DynamicTextData data;

    private List<EnemyStats> enemies = new List<EnemyStats>();

    [Header("Slash Attack Parameters")]
    private double damage;
    public double initialSlashMultiplier = 1.2;
    public double progressiveSlashMultiplier = 0.8;
    public double finalSlashMultiplier = 1.5;

    [Header("Slash Types")]
    public bool performInitialSlash = true;  // Option to enable/disable initial slash
    public bool performFinalSlash = true;    // Option to enable/disable final slash

    [Header("Slash Rotation")]
    public bool useRandomRotation = true;
    public double fixedRotationAngle = 45.0;
    public float randomRotationMinAngle = -60.0f;
    public float randomRotationMaxAngle = 60.0f;

    [Header("Timing and Animation")]
    public int numberOfSlashes = 10;
    public double progressiveSlashDelay = 0.2;
    public double finalSlashDelay = 0.5;
    public double slashInterval = 0.1;
    public AnimationCurve slashSpeedCurve;
    public AnimationCurve slashDamageCurve;

    [Header("Screen Shake Settings")]
    [SerializeField] private bool screenShake;
    [SerializeField] private double shakeDuration = 0.2;
    [SerializeField] private double shakeMagnitude = 0.1;

    [Header("Flashbang Settings")]
    [SerializeField] private bool isFlashbang;
    [SerializeField] private GameObject flashbangOverlay;
    [SerializeField] private double flashDuration = 0.15;
    [SerializeField] private Color flashColor = Color.white;

    [Header("Random Position Settings")]
    [SerializeField] private double randomPositionRadius = 0.5; // Maximum distance from target
    [SerializeField] private bool useRandomPosition = true; // Toggle for random positioning

    [Header("References")]
    private Transform enemyTarget;
    public Transform slashEffectPrefab; // Optional visual slash effect
    public Transform FinalSlash;
    public PartialFlashbangEffect flashbang;

    [Header("Debug")]
    [SerializeField] private bool logDamageValues = true; // Toggle for debug logging
    [SerializeField] private bool debugMode = true;

    private void Start()
    {
        if (debugMode)
        {
            Debug.Log("MultiSlash: Starting execution");
        }

        // Initialize damage calculation with fallback values
        CalculateDamage();

        // Setup animation curves with fallbacks
        SetupCurves();

        // Find enemies and start attack if any are found
        FindEnemiesAndAttack();

        // Find flashbang effect
        if (flashbang == null)
        {
            flashbang = FindFirstObjectByType<PartialFlashbangEffect>();
        }
    }

    /// <summary>
    /// Calculate damage based on power, buffs, and character stats
    /// </summary>
    private void CalculateDamage()
    {
        // Set default damage value to avoid potential NaN
        damage = 10.0; // Higher default as a fallback

        // Validate character stats to prevent division by zero or invalid calculations
        if (characterAtk <= 0)
        {
            characterAtk = 10.0;
            if (debugMode) Debug.LogWarning("MultiSlash: characterAtk was <= 0, using default value of 10");
        }
        if (characterCritMulti <= 0)
        {
            characterCritMulti = 1.5;
            if (debugMode) Debug.LogWarning("MultiSlash: characterCritMulti was <= 0, using default value of 1.5");
        }

        // Validate power and buff values to prevent NaN
        if (power <= 0)
        {
            power = 100.0;
            if (debugMode) Debug.LogWarning("MultiSlash: power was <= 0, using default value of 100");
        }

        try
        {
            bool isCrit = false;
            // Make sure characterCritPerc is a valid percentage
            double validCritPerc = System.Math.Clamp(characterCritPerc, 0.0, 100.0);

            if (Random.value <= (validCritPerc / 100.0))
            {
                data = critData;
                damage = (power / 100.0) * characterCritMulti * (1.0 + (buff / 100.0)) * characterAtk;
                isCrit = true;
            }
            else
            {
                data = normData;
                damage = (power / 100.0) * (1.0 + (buff / 100.0)) * characterAtk;
            }

            // Validate damage calculation
            if (double.IsNaN(damage) || double.IsInfinity(damage))
            {
                string damageFormula = isCrit ?
                    $"(power({power})/100) * characterCritMulti({characterCritMulti}) * (1 + (buff({buff})/100)) * characterAtk({characterAtk})" :
                    $"(power({power})/100) * (1 + (buff({buff})/100)) * characterAtk({characterAtk})";

                Debug.LogWarning($"MultiSlash: Invalid damage calculation detected in formula: {damageFormula}");
                damage = characterAtk * 0.5; // Fallback to a reasonable value based on character attack
            }

            if (logDamageValues || debugMode)
            {
                Debug.Log($"MultiSlash: Calculated damage = {damage}, power = {power}, buff = {buff}, characterAtk = {characterAtk}, isCrit = {isCrit}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MultiSlash: Error in damage calculation: {e.Message}");
            damage = characterAtk * 0.5; // Fallback value
        }
    }

    /// <summary>
    /// Setup animation curves with default values if not set
    /// </summary>
    private void SetupCurves()
    {
        if (slashDamageCurve == null || slashDamageCurve.keys.Length == 0)
        {
            if (debugMode) Debug.LogWarning("MultiSlash: slashDamageCurve was null or empty, creating default curve");
            slashDamageCurve = new AnimationCurve();
            slashDamageCurve.AddKey(0f, 0.5f);
            slashDamageCurve.AddKey(1f, 1f);
        }

        if (slashSpeedCurve == null || slashSpeedCurve.keys.Length == 0)
        {
            if (debugMode) Debug.LogWarning("MultiSlash: slashSpeedCurve was null or empty, creating default curve");
            slashSpeedCurve = new AnimationCurve();
            slashSpeedCurve.AddKey(0f, 0.8f);
            slashSpeedCurve.AddKey(1f, 1.2f);
        }
    }

    /// <summary>
    /// Find all enemies in range and start attack sequence
    /// </summary>
    private void FindEnemiesAndAttack()
    {
        if (debugMode) Debug.Log($"MultiSlash: Looking for enemies in range {range} from position {transform.position}");

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, (float)range);
        enemies.Clear();

        if (hitColliders.Length == 0 && debugMode)
        {
            Debug.LogWarning("MultiSlash: No colliders found in range!");
        }

        foreach (Collider2D hitcollider in hitColliders)
        {
            if (hitcollider == null) continue;

            EnemyStats enemy = hitcollider.gameObject.GetComponent<EnemyStats>();
            if (enemy != null && enemy.gameObject.tag != shooter.tag)
            {
                enemies.Add(enemy);
                if (debugMode) Debug.Log($"MultiSlash: Found enemy: {enemy.gameObject.name}");
            }
        }

        if (enemies.Count > 0)
        {
            enemyTarget = enemies[0].transform;
            if (enemyTarget.GetComponent<Enemy>() != null)
            {
                enemyTarget.GetComponent<Enemy>().moving = false;
                if (debugMode) Debug.Log("MultiSlash: Stopped enemy movement");
            }
            StartCoroutine(PerformMultiSlashAttack());
            if (debugMode) Debug.Log("MultiSlash: Started attack sequence");
        }
        else
        {
            if (debugMode) Debug.LogWarning("MultiSlash: No valid enemies found to attack!");
            // Self-destroy if no enemies found
            Destroy(gameObject, 0.5f);
        }
    }

    /// <summary>
    /// Main attack sequence - initial, progressive, and final slashes
    /// </summary>
    public IEnumerator PerformMultiSlashAttack()
    {
        if (debugMode) Debug.Log("MultiSlash: Beginning attack sequence");

        // Initial large slash (optional)
        if (performInitialSlash)
        {
            if (debugMode) Debug.Log("MultiSlash: Performing initial slash");
            yield return StartCoroutine(PerformInitialSlash());
        }

        // Series of progressive slashes (always performed)
        if (debugMode) Debug.Log($"MultiSlash: Performing {numberOfSlashes} progressive slashes");
        yield return StartCoroutine(PerformProgressiveSlashes());

        // Final massive slash (optional)
        if (performFinalSlash)
        {
            if (debugMode) Debug.Log("MultiSlash: Waiting for final slash");
            yield return new WaitForSeconds((float)finalSlashDelay);
            if (debugMode) Debug.Log("MultiSlash: Performing final slash");
            yield return StartCoroutine(PerformFinalSlash());
        }

        // If no enemy was ever found or if it was destroyed during the attack
        if (enemyTarget != null && enemyTarget.GetComponent<Enemy>() != null)
        {
            enemyTarget.GetComponent<Enemy>().moving = true;
            if (debugMode) Debug.Log("MultiSlash: Resumed enemy movement");
        }

        // Check if this GameObject should be destroyed after the attack
        if (debugMode) Debug.Log("MultiSlash: Attack sequence complete, destroying gameObject");
    }

    /// <summary>
    /// Perform initial powerful slash
    /// </summary>
    IEnumerator PerformInitialSlash()
    {
        // Validate damage before applying
        double initialDamage = ValidateDamageValue(damage * initialSlashMultiplier);

        if (debugMode) Debug.Log($"MultiSlash: Initial slash damage = {initialDamage}");

        // Apply damage
        ApplyDamageToEnemy(initialDamage, Vector3.zero);

        yield return new WaitForSeconds((float)progressiveSlashDelay);
    }

    /// <summary>
    /// Perform series of progressive slashes with scaling damage and speed
    /// </summary>
    IEnumerator PerformProgressiveSlashes()
    {
        // Ensure we have a valid number of slashes
        if (numberOfSlashes <= 0)
        {
            numberOfSlashes = 3;
            if (debugMode) Debug.LogWarning("MultiSlash: numberOfSlashes was <= 0, using default value of 3");
        }

        if (debugMode) Debug.Log($"MultiSlash: Starting progressive slashes sequence with {numberOfSlashes} slashes");

        for (int i = 0; i < numberOfSlashes; i++)
        {
            // Calculate progressive parameters - avoid division by zero
            float progressRatio = (numberOfSlashes > 1) ? (float)i / (numberOfSlashes - 1) : 0;

            // Calculate rotation
            Vector3 slashRotation = CalculateSlashRotation(i);

            // Calculate and validate damage
            double curveValue = slashDamageCurve != null ?
                Mathf.Clamp01(slashDamageCurve.Evaluate(progressRatio)) : 0.8;

            double currentDamage = ValidateDamageValue(damage * progressiveSlashMultiplier * curveValue);

            // Calculate slash speed (with validation)
            float slashSpeed = slashSpeedCurve != null ?
                Mathf.Max(0.1f, slashSpeedCurve.Evaluate(progressRatio)) : 1.0f;

            if (debugMode) Debug.Log($"MultiSlash: Progressive slash {i + 1}/{numberOfSlashes}, damage={currentDamage}, speed={slashSpeed}");

            // Spawn the visual effect
            SpawnSlashEffect(slashRotation);

            // Apply damage
            ApplyDamageToEnemy(currentDamage, slashRotation);

            // Wait for next slash - ensure we don't divide by zero
            float waitTime = (float)slashInterval / Mathf.Max(0.1f, slashSpeed);
            yield return new WaitForSeconds(waitTime);
        }

        if (debugMode) Debug.Log("MultiSlash: Progressive slashes sequence completed");
    }

    /// <summary>
    /// Perform final powerful slash with screen shake and flashbang
    /// </summary>
    IEnumerator PerformFinalSlash()
    {
        // Calculate rotation
        Vector3 finalSlashRotation = useRandomRotation
            ? new Vector3(0, 0, (float)Random.Range(randomRotationMinAngle, randomRotationMaxAngle))
            : new Vector3(0, 0, (float)(fixedRotationAngle * 2));

        // Validate damage
        double finalDamage = ValidateDamageValue(damage * finalSlashMultiplier);

        if (debugMode) Debug.Log($"MultiSlash: Final slash damage = {finalDamage}");

        // Apply damage
        ApplyDamageToEnemy(finalDamage, finalSlashRotation);

        // Instantiate final slash visual
        if (FinalSlash != null && enemyTarget != null)
        {
            Instantiate(FinalSlash, enemyTarget.position, Quaternion.Euler(finalSlashRotation));
        }
        else if (debugMode && FinalSlash == null)
        {
            Debug.LogWarning("MultiSlash: FinalSlash prefab is null");
        }

        // Trigger screen shake and flashbang simultaneously
        StartCoroutine(ScreenShake());
        if (flashbang != null && isFlashbang)
        {
            flashbang.flashDuration = (float)flashDuration;
            flashbang.flashColor = flashColor;
            flashbang.TriggerFlashbang();
        }
        else if (debugMode)
        {
            Debug.LogWarning("MultiSlash: flashbang reference is null");
        }

        yield return new WaitForSeconds((float)progressiveSlashDelay);
    }

    /// <summary>
    /// Calculate rotation for slash visual effect
    /// </summary>
    Vector3 CalculateSlashRotation(int slashIndex)
    {
        if (!useRandomRotation)
        {
            // Fixed rotation pattern
            return new Vector3(0, 0, (float)(fixedRotationAngle * slashIndex));
        }
        else
        {
            // Random rotation for each slash
            return new Vector3(0, 0, (float)Random.Range(randomRotationMinAngle, randomRotationMaxAngle));
        }
    }

    /// <summary>
    /// Validate damage value to prevent NaN or Infinity
    /// </summary>
    double ValidateDamageValue(double damageValue)
    {
        // Check for invalid damage values
        if (double.IsNaN(damageValue) || double.IsInfinity(damageValue))
        {
            if (logDamageValues || debugMode)
            {
                Debug.LogWarning($"MultiSlash: Invalid damage value {damageValue} detected. Using fallback value.");
            }
            return System.Math.Max(1.0, characterAtk * 0.5); // Fallback to reasonable value
        }

        // Ensure minimum damage
        return System.Math.Max(0.1, damageValue);
    }

    /// <summary>
    /// Apply damage to enemy target
    /// </summary>
    void ApplyDamageToEnemy(double damageAmount, Vector3 slashRotation)
    {
        if (enemyTarget == null)
        {
            if (debugMode) Debug.LogWarning("MultiSlash: Enemy target is null in ApplyDamageToEnemy");
            return;
        }

        EnemyStats enemyStats = enemyTarget.GetComponent<EnemyStats>();
        if (enemyStats != null)
        {
            // Final safety check before applying damage
            double safeValue = ValidateDamageValue(damageAmount);

            if (logDamageValues || debugMode)
            {
                Debug.Log($"MultiSlash: Applying damage: {BigNumberFormatter.Format(safeValue, 1)} to {enemyTarget.name}");
            }
            enemyStats.TakeDamage(safeValue);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"MultiSlash: Enemy target {enemyTarget.name} has no EnemyStats component");
        }
    }

    /// <summary>
    /// Get random position around a center point
    /// </summary>
    Vector3 GetRandomPositionAroundPoint(Vector3 centerPoint)
    {
        // Generate random point in circle
        float angle = Random.Range(0f, Mathf.PI * 2);
        float distance = Random.Range(0f, (float)randomPositionRadius);

        // Convert polar coordinates to Cartesian
        float x = Mathf.Cos(angle) * distance;
        float y = Mathf.Sin(angle) * distance;

        // Return center + offset
        return new Vector3(
            centerPoint.x + x,
            centerPoint.y + y,
            centerPoint.z
        );
    }

    /// <summary>
    /// Spawn visual slash effect at target
    /// </summary>
    void SpawnSlashEffect(Vector3 rotation)
    {
        if (slashEffectPrefab == null)
        {
            if (debugMode) Debug.LogWarning("MultiSlash: slashEffectPrefab is null");
            return;
        }

        if (enemyTarget == null)
        {
            if (debugMode) Debug.LogWarning("MultiSlash: enemyTarget is null when trying to spawn slash effect");
            return;
        }

        // Calculate position
        Vector3 spawnPosition;

        if (useRandomPosition)
        {
            // Get random position around enemy target
            spawnPosition = GetRandomPositionAroundPoint(enemyTarget.position);
        }
        else
        {
            // Use exact enemy position
            spawnPosition = enemyTarget.position;
        }

        // Instantiate slash effect
        Transform slashEffect = Instantiate(
            slashEffectPrefab,
            spawnPosition,
            Quaternion.Euler(rotation)
        );

        if (debugMode) Debug.Log($"MultiSlash: Spawned slash effect at position {spawnPosition}");
    }

    /// <summary>
    /// Apply screen shake effect to camera
    /// </summary>
    private IEnumerator ScreenShake()
    {
        if (screenShake)
        {
            if (Camera.main == null)
            {
                if (debugMode) Debug.LogWarning("MultiSlash: Main camera is null, cannot perform screen shake");
                yield break;
            }

            Vector3 originalPosition = Camera.main.transform.localPosition;
            float elapsed = 0f;

            if (debugMode) Debug.Log("MultiSlash: Starting screen shake");

            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * (float)shakeMagnitude;
                float y = Random.Range(-1f, 1f) * (float)shakeMagnitude;

                Camera.main.transform.localPosition = new Vector3(
                    originalPosition.x + x,
                    originalPosition.y + y,
                    originalPosition.z
                );

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Reset camera position
            Camera.main.transform.localPosition = originalPosition;

            if (debugMode) Debug.Log("MultiSlash: Completed screen shake");
        }
    }
}