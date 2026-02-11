using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSystem : MonoBehaviour
{
    [System.Serializable]
    public class EnemyType
    {
        [Tooltip("The enemy prefab to spawn")]
        public GameObject enemyPrefab;

        [Tooltip("Name identifier for this enemy type")]
        public string enemyName;

        [Tooltip("The first wave this enemy will appear in each cycle (1-based)")]
        public int firstAppearanceWave;

        [Tooltip("Reference to base stats for this enemy type")]
        public EnemyStats stats;

        [Tooltip("Budget cost to spawn this enemy (higher = rarer)")]
        public int cost = 10;

        [Tooltip("Spawning weight multiplier (higher = more frequent)")]
        [Range(0.1f, 3.0f)]
        public float spawnWeight = 1.0f;

        [Tooltip("Enemy faction/type that determines special behaviors")]
        public EnemyFaction faction = EnemyFaction.Standard;
    }

    public enum EnemyFaction
    {
        Standard,
        Elite,
        Swarm,
        Miniboss,
        Support
    }

    [System.Serializable]
    public class SpecialWave
    {
        [Tooltip("Wave number to apply this special wave (0 = disabled)")]
        public int waveNumber;

        [Tooltip("Theme/name of this special wave")]
        public string waveName;

        [Tooltip("Description of this special wave's mechanics")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Enemy faction to focus on for this wave")]
        public EnemyFaction primaryFaction;

        [Tooltip("Budget multiplier for this wave (1.0 = normal)")]
        [Range(0.5f, 3.0f)]
        public float budgetMultiplier = 1.5f;

        [Tooltip("Gold/XP reward multiplier (1.0 = normal)")]
        [Range(1.0f, 5.0f)]
        public float rewardMultiplier = 1.3f;

        [Tooltip("Override the normal mini-wave count")]
        public int customMiniWaveCount = 0;
    }

    [Header("Wave Settings")]
    [Tooltip("Current wave number")]
    [SerializeField] private int currentWave = 1;

    [Tooltip("Number of waves that make up one complete cycle")]
    [SerializeField] private int wavesPerCycle = 10;

    [Tooltip("Base position for spawning enemies (if not using screen-based spawning)")]
    [SerializeField] private Vector3 spawnPosition;

    [Tooltip("Radius around spawn position to randomly place enemies")]
    [SerializeField] private float spawnRadius = 5f;

    [SerializeField] private float waveDelay = 3;

    [Header("Exponential Enemy Scaling")]
    [Tooltip("Base enemy HP at wave 1")]
    [SerializeField] private double baseEnemyHealth = 100.0;

    [Tooltip("Base enemy ATK at wave 1")]
    [SerializeField] private double baseEnemyAttack = 10.0;

    [Tooltip("Base enemy DEF at wave 1")]
    [SerializeField] private double baseEnemyDefense = 5.0;

    [Tooltip("Growth multiplier per wave (1.15 = +15% per wave)")]
    [Range(1.05f, 1.25f)]
    [SerializeField] private float enemyGrowthRate = 1.15f;

    [Header("Exponential Reward Scaling")]
    [Tooltip("Base gold reward at wave 1")]
    [SerializeField] private double baseGoldReward = 50.0;

    [Tooltip("Gold growth rate per wave (1.18 = +18% per wave)")]
    [Range(1.10f, 1.30f)]
    [SerializeField] private float goldGrowthRate = 1.18f;

    [Tooltip("Base exp reward at wave 1")]
    [SerializeField] private double baseExpReward = 20.0;

    [Tooltip("Exp growth rate per wave (1.16 = +16% per wave)")]
    [Range(1.10f, 1.30f)]
    [SerializeField] private float expGrowthRate = 1.16f;

    [Tooltip("Drop multiplier growth rate")]
    [Range(1.05f, 1.20f)]
    [SerializeField] private float dropGrowthRate = 1.10f;

    [Header("Budget Settings")]
    [Tooltip("Starting budget for wave 1")]
    [SerializeField] private int baseWaveBudget = 50;

    [Tooltip("How much the wave budget increases with each wave")]
    [SerializeField] private float waveBudgetIncreasePerWave = 15f;

    [Tooltip("Maximum budget for any wave")]
    [SerializeField] private int maxWaveBudget = 1000;

    [Tooltip("Percentage chance for bonus budget in any wave (0-100)")]
    [Range(0, 100)]
    [SerializeField] private float bonusBudgetChance = 15f;

    [Tooltip("Bonus budget multiplier when triggered")]
    [Range(1.1f, 2.0f)]
    [SerializeField] private float bonusBudgetMultiplier = 1.3f;

    [Header("Enemy Management")]
    [Tooltip("List of all available enemy types that can spawn")]
    [SerializeField] private List<EnemyType> enemyTypes = new List<EnemyType>();

    [Tooltip("Randomize which enemies are selected for spawning")]
    [SerializeField] private bool randomizeEnemySelection = true;

    [Tooltip("Chance for elite version of standard enemies (0-100%)")]
    [Range(0, 100)]
    [SerializeField] private float eliteEnemyChance = 5f;

    [Tooltip("Stat multiplier for elite enemies")]
    [Range(1.5f, 5.0f)]
    [SerializeField] private float eliteStatMultiplier = 2.5f;

    [Tooltip("Reward multiplier for elite enemies")]
    [Range(1.5f, 5.0f)]
    [SerializeField] private float eliteRewardMultiplier = 3.0f;

    [Header("Mini-Wave Settings")]
    [Tooltip("How many sub-waves make up each full wave")]
    [SerializeField] private int miniWavesPerWave = 3;

    [Tooltip("Minimum time between mini-waves")]
    [SerializeField] private float minMiniWaveDelay = 5f;

    [Tooltip("Maximum time between mini-waves")]
    [SerializeField] private float maxMiniWaveDelay = 15f;

    [Tooltip("Randomize mini-wave budget distribution")]
    [SerializeField] private bool randomizeMiniWaves = true;

    [Tooltip("Focus first mini-wave on weaker enemies, last on stronger ones")]
    [SerializeField] private bool progressiveMiniWaveDifficulty = false;

    [Header("Spawn Settings")]
    [Tooltip("Reference to the main camera for screen-based spawning")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("How far beyond the screen edge to spawn enemies")]
    [SerializeField] private float spawnOffsetFromScreen = 2f;

    [Tooltip("Random variation in vertical spawn position")]
    [SerializeField] private float spawnVerticalRandom = 3f;

    [SerializeField] private float minEnemySpawnDelay = 0.05f;
    [SerializeField] private float maxEnemySpawnDelay = 0.2f;

    [Tooltip("Use wave-specific spawn sides (not just right side)")]
    [SerializeField] private bool useMultiDirectionalSpawning = false;

    [Tooltip("Chance to spawn enemy from left, top or bottom (0-100%)")]
    [Range(0, 100)]
    [SerializeField] private float alternateSpawnSideChance = 30f;

    [Header("Special Waves")]
    [Tooltip("List of special wave configurations with unique properties")]
    [SerializeField] private List<SpecialWave> specialWaves = new List<SpecialWave>();

    [Tooltip("Chance for a random special wave to occur (0-100%)")]
    [Range(0, 100)]
    [SerializeField] private float randomSpecialWaveChance = 10f;

    [Tooltip("Minimum wave number before random special waves can occur")]
    [SerializeField] private int minWaveForRandomSpecial = 5;

    [Header("Boss Waves")]
    [Tooltip("Enable boss waves at the end of each cycle")]
    [SerializeField] private bool enableBossWaves = true;

    [Tooltip("Prefab for boss enemy")]
    [SerializeField] private GameObject bossPrefab;

    [Tooltip("Additional stat scaling for bosses")]
    [Range(1.0f, 5.0f)]
    [SerializeField] private float bossStatMultiplier = 3.0f;

    [Tooltip("Additional reward scaling for bosses")]
    [Range(1.0f, 10.0f)]
    [SerializeField] private float bossRewardMultiplier = 5.0f;

    [Header("Debug Options")]
    [Tooltip("Use exact base stats for wave 1, cycle 1 enemies")]
    [SerializeField] private bool useExactBaseStatsForFirstWave = true;

    [Tooltip("Display debug logs for wave statistics")]
    [SerializeField] private bool showDetailedLogs = true;

    [Tooltip("Skip directly to a specific wave when testing")]
    [SerializeField] private int debugStartWave = 1;

    [Tooltip("Override specific special wave for testing")]
    [SerializeField] private int debugForceSpecialWave = 0;

    // Private variables
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int currentCycle = 0;
    private int waveBudgetRemaining = 0;
    private bool isWaveInProgress = false;
    private Coroutine waveCoroutine;
    private bool isBossWave = false;
    private SpecialWave currentSpecialWave = null;

    // Event delegates
    public delegate void WaveEvent(int waveNumber);
    public event WaveEvent OnWaveStarted;
    public event WaveEvent OnWaveCompleted;
    public delegate void MiniWaveEvent(int waveNumber, int miniWaveCount);
    public event MiniWaveEvent OnMiniWaveStarted;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (debugStartWave > 1)
            currentWave = debugStartWave;

        StartWave(currentWave);
    }

    public void SetWave(int waveNumber)
    {
        if (waveNumber < 1) waveNumber = 1;

        currentWave = waveNumber;
        ClearExistingEnemies();
        StartWave(currentWave);
    }

    private void ClearExistingEnemies()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        activeEnemies.Clear();
        isWaveInProgress = false;
    }

    // ============================================
    // PLAYER DEATH HANDLER - RESTARTS CURRENT WAVE
    // ============================================
    public void NotifyPlayerDeath()
    {
        if (showDetailedLogs)
            Debug.Log($"Player died! Restarting wave {currentWave}...");

        // Clear all enemies
        ClearExistingEnemies();

        // Restart the same wave
        StartWave(currentWave);
    }

    private void StartWave(int waveNumber)
    {
        if (isWaveInProgress)
        {
            ClearExistingEnemies();
        }

        // Calculate which cycle we're in
        currentCycle = (waveNumber - 1) / wavesPerCycle;

        // Calculate the current wave within the cycle (1-based)
        int waveInCycle = waveNumber - (currentCycle * wavesPerCycle);
        if (waveInCycle == 0)
        {
            waveInCycle = wavesPerCycle;
            currentCycle--;
        }

        // Check if this is a boss wave (last wave in cycle)
        isBossWave = enableBossWaves && waveInCycle == wavesPerCycle;

        // Check if this is a special wave
        currentSpecialWave = null;

        // First check scheduled special waves
        foreach (SpecialWave specialWave in specialWaves)
        {
            if (specialWave.waveNumber == waveNumber ||
                (debugForceSpecialWave > 0 && specialWave.waveNumber == debugForceSpecialWave))
            {
                currentSpecialWave = specialWave;
                break;
            }
        }

        // Then check for random special waves if appropriate
        if (currentSpecialWave == null &&
            waveNumber >= minWaveForRandomSpecial &&
            !isBossWave &&
            Random.Range(0f, 100f) < randomSpecialWaveChance)
        {
            if (specialWaves.Count > 0)
            {
                currentSpecialWave = specialWaves[Random.Range(0, specialWaves.Count)];

                if (showDetailedLogs)
                    Debug.Log($"Random special wave triggered: {currentSpecialWave.waveName}");
            }
        }

        // Calculate wave budget
        waveBudgetRemaining = CalculateWaveBudget(waveNumber);

        // Apply special wave budget multiplier
        if (currentSpecialWave != null)
        {
            waveBudgetRemaining = Mathf.RoundToInt(waveBudgetRemaining * currentSpecialWave.budgetMultiplier);

            if (showDetailedLogs)
                Debug.Log($"Special wave '{currentSpecialWave.waveName}' budget: {waveBudgetRemaining}");
        }

        // Apply bonus budget chance
        if (Random.Range(0f, 100f) < bonusBudgetChance && !isBossWave)
        {
            int originalBudget = waveBudgetRemaining;
            waveBudgetRemaining = Mathf.RoundToInt(waveBudgetRemaining * bonusBudgetMultiplier);

            if (showDetailedLogs)
                Debug.Log($"Bonus budget triggered! {originalBudget} → {waveBudgetRemaining}");
        }

        isWaveInProgress = true;

        // Start the wave with delay
        if (isBossWave)
        {
            waveCoroutine = StartCoroutine(StartWaveWithDelay(RunBossWaveSequence));
        }
        else
        {
            waveCoroutine = StartCoroutine(StartWaveWithDelay(() => RunWaveSequence(waveInCycle)));
        }
    }

    private IEnumerator StartWaveWithDelay(System.Func<IEnumerator> waveSequence)
    {
        yield return new WaitForSeconds(waveDelay);

        OnWaveStarted?.Invoke(currentWave);

        if (showDetailedLogs)
        {
            string waveType = isBossWave ? "BOSS WAVE" : (currentSpecialWave != null ? $"SPECIAL WAVE ({currentSpecialWave.waveName})" : "Wave");
            Debug.Log($"{waveType} {currentWave} (Cycle {currentCycle + 1}) started - Budget: {waveBudgetRemaining}");
        }

        yield return StartCoroutine(waveSequence());
    }

    private int CalculateWaveBudget(int waveNumber)
    {
        // Simple progressive budget calculation
        float calculatedBudget = baseWaveBudget + (waveNumber - 1) * waveBudgetIncreasePerWave;

        // Add multiplier based on cycle
        calculatedBudget *= (1 + currentCycle * 0.2f);

        return Mathf.Min(Mathf.RoundToInt(calculatedBudget), maxWaveBudget);
    }

    private IEnumerator RunWaveSequence(int waveInCycle)
    {
        List<EnemyType> availableEnemies = GetAvailableEnemyTypes(waveInCycle);

        if (availableEnemies.Count == 0)
        {
            Debug.LogError("No enemy types available for current wave");
            isWaveInProgress = false;
            yield break;
        }

        // Determine mini-wave count
        int miniWaveCount = miniWavesPerWave;

        if (currentSpecialWave != null && currentSpecialWave.customMiniWaveCount > 0)
            miniWaveCount = currentSpecialWave.customMiniWaveCount;

        // Distribute budget between mini-waves
        List<int> miniWaveBudgets = DistributeBudgetToMiniWaves(waveBudgetRemaining, miniWaveCount);

        // Filter enemies for special waves
        if (currentSpecialWave != null && currentSpecialWave.primaryFaction != EnemyFaction.Standard)
        {
            availableEnemies = FilterEnemiesByFaction(availableEnemies, currentSpecialWave.primaryFaction);

            if (availableEnemies.Count == 0)
            {
                Debug.LogWarning($"No enemies found for faction {currentSpecialWave.primaryFaction}, using all available enemies");
                availableEnemies = GetAvailableEnemyTypes(waveInCycle);
            }
        }

        for (int i = 0; i < miniWaveBudgets.Count; i++)
        {
            int miniWaveBudget = miniWaveBudgets[i];

            SpawnDirection direction = SpawnDirection.Right;

            if (useMultiDirectionalSpawning && Random.Range(0f, 100f) < alternateSpawnSideChance)
            {
                direction = (SpawnDirection)Random.Range(0, 4);
            }

            yield return StartCoroutine(SpawnMiniWave(availableEnemies, miniWaveBudget, waveInCycle, i, miniWaveCount, direction));

            if (showDetailedLogs)
                Debug.Log($"Mini-wave {i + 1}/{miniWaveBudgets.Count} - Budget: {miniWaveBudget}");

            if (i < miniWaveBudgets.Count - 1)
            {
                float waitTime = Random.Range(minMiniWaveDelay, maxMiniWaveDelay);
                yield return new WaitForSeconds(waitTime);
            }
        }

        yield return StartCoroutine(WaitForAllEnemiesDefeated());

        isWaveInProgress = false;

        if (showDetailedLogs)
            Debug.Log("Wave completed!");

        OnWaveCompleted?.Invoke(currentWave);

        yield return new WaitForSeconds(waveDelay);

        AdvanceToNextWave();
    }

    private List<EnemyType> FilterEnemiesByFaction(List<EnemyType> allEnemies, EnemyFaction faction)
    {
        List<EnemyType> filtered = new List<EnemyType>();

        foreach (EnemyType enemy in allEnemies)
        {
            if (enemy.faction == faction)
                filtered.Add(enemy);
        }

        return filtered;
    }

    private enum SpawnDirection
    {
        Right,
        Left,
        Top,
        Bottom
    }

    private IEnumerator RunBossWaveSequence()
    {
        if (bossPrefab == null)
        {
            Debug.LogError("Boss prefab not assigned but boss wave triggered!");
            isWaveInProgress = false;
            yield break;
        }

        Vector3 bossSpawnPos = GetSpawnPositionOffScreen(SpawnDirection.Right, true);

        GameObject boss = Instantiate(bossPrefab, bossSpawnPos, bossPrefab.transform.rotation);

        ApplyBossStatScaling(boss);
        ApplyGoldScaling(boss, bossRewardMultiplier);
        ApplyExpScaling(boss, bossRewardMultiplier);
        ApplyDropScaling(boss, bossRewardMultiplier);

        activeEnemies.Add(boss);

        yield return StartCoroutine(WaitForAllEnemiesDefeated());

        isWaveInProgress = false;

        if (showDetailedLogs)
            Debug.Log("Boss defeated! Advancing to next cycle.");

        OnWaveCompleted?.Invoke(currentWave);

        AdvanceToNextWave();
    }

    private void ApplyBossStatScaling(GameObject boss)
    {
        EnemyStats bossStats = boss.GetComponent<EnemyStats>();

        if (bossStats == null)
        {
            Debug.LogWarning("Boss missing EnemyStats component!");
            return;
        }

        // Boss uses exponential formula + boss multiplier
        double waveMultiplier = System.Math.Pow(enemyGrowthRate, currentWave - 1);

        double bossHealth = baseEnemyHealth * waveMultiplier * bossStatMultiplier;
        double bossAttack = baseEnemyAttack * waveMultiplier * bossStatMultiplier;
        double bossDefense = baseEnemyDefense * waveMultiplier * bossStatMultiplier;

        bossStats.maxHealth = (float)bossHealth;
        bossStats.currentHealth = (float)bossHealth;
        bossStats.attack = (float)bossAttack;
        bossStats.defense = (float)bossDefense;

        // Visual distinction
        SpriteRenderer sr = boss.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 0.2f, 0.2f);
            sr.sortingOrder += 10;
        }

        Debug.Log($"=== BOSS WAVE {currentWave} ===");
        Debug.Log($"Boss HP: {BigNumberFormatter.Format(bossHealth)}");
        Debug.Log($"Boss ATK: {BigNumberFormatter.Format(bossAttack)}");
    }

    private void AdvanceToNextWave()
    {
        currentWave++;
        StartWave(currentWave);
    }

    private IEnumerator WaitForAllEnemiesDefeated()
    {
        while (activeEnemies.Count > 0)
        {
            activeEnemies.RemoveAll(enemy => enemy == null);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private List<EnemyType> GetAvailableEnemyTypes(int waveInCycle)
    {
        List<EnemyType> availableTypes = new List<EnemyType>();

        foreach (EnemyType enemyType in enemyTypes)
        {
            if (enemyType.firstAppearanceWave <= waveInCycle)
            {
                availableTypes.Add(enemyType);
            }
        }

        if (randomizeEnemySelection && availableTypes.Count > 1)
        {
            // Fisher-Yates shuffle
            for (int i = availableTypes.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                EnemyType temp = availableTypes[i];
                availableTypes[i] = availableTypes[j];
                availableTypes[j] = temp;
            }
        }

        return availableTypes;
    }

    private List<int> DistributeBudgetToMiniWaves(int totalBudget, int miniWaveCount)
    {
        List<int> budgets = new List<int>();

        if (randomizeMiniWaves)
        {
            float[] weights = new float[miniWaveCount];
            float totalWeight = 0;

            for (int i = 0; i < miniWaveCount; i++)
            {
                if (progressiveMiniWaveDifficulty)
                {
                    weights[i] = 0.5f + ((float)i / miniWaveCount);
                }
                else
                {
                    weights[i] = Random.Range(0.5f, 1.5f);
                }
                totalWeight += weights[i];
            }

            int remainingBudget = totalBudget;
            for (int i = 0; i < miniWaveCount; i++)
            {
                if (i == miniWaveCount - 1)
                {
                    budgets.Add(remainingBudget);
                }
                else
                {
                    int waveBudget = Mathf.RoundToInt((weights[i] / totalWeight) * totalBudget);
                    budgets.Add(waveBudget);
                    remainingBudget -= waveBudget;
                }
            }
        }
        else
        {
            int baseAmount = totalBudget / miniWaveCount;
            int remainder = totalBudget % miniWaveCount;

            for (int i = 0; i < miniWaveCount; i++)
            {
                if (i < remainder)
                    budgets.Add(baseAmount + 1);
                else
                    budgets.Add(baseAmount);
            }
        }

        return budgets;
    }

    private IEnumerator SpawnMiniWave(List<EnemyType> availableEnemies, int budget, int waveInCycle, int miniWaveIndex, int totalMiniWaves, SpawnDirection direction)
    {
        if (availableEnemies.Count == 0 || budget <= 0)
            yield break;

        int remainingBudget = budget;

        List<float> spawnWeights = new List<float>();
        float totalWeight = 0;

        foreach (EnemyType enemy in availableEnemies)
        {
            float weight = enemy.spawnWeight;
            spawnWeights.Add(weight);
            totalWeight += weight;
        }

        OnMiniWaveStarted?.Invoke(currentWave, miniWaveIndex);

        while (remainingBudget > 0)
        {
            bool canSpawnAny = false;
            foreach (EnemyType enemy in availableEnemies)
            {
                if (enemy.cost <= remainingBudget)
                {
                    canSpawnAny = true;
                    break;
                }
            }

            if (!canSpawnAny)
                break;

            int selectedIndex = GetWeightedRandomIndex(spawnWeights, totalWeight);
            if (selectedIndex < 0 || selectedIndex >= availableEnemies.Count)
                break;

            EnemyType selectedEnemy = availableEnemies[selectedIndex];

            if (selectedEnemy.cost <= remainingBudget)
            {
                bool isElite = (selectedEnemy.faction != EnemyFaction.Elite) &&
                             (Random.Range(0f, 100f) < eliteEnemyChance);

                Vector3 spawnPos = GetSpawnPositionOffScreen(direction, false);
                GameObject enemy = Instantiate(selectedEnemy.enemyPrefab, spawnPos, selectedEnemy.enemyPrefab.transform.rotation);

                ApplyStatScaling(enemy, selectedEnemy, isElite);

                float finalGoldMultiplier = isElite ? eliteRewardMultiplier : 1.0f;
                float finalExpMultiplier = isElite ? eliteRewardMultiplier : 1.0f;
                float finalDropMultiplier = isElite ? eliteRewardMultiplier : 1.0f;

                ApplyGoldScaling(enemy, finalGoldMultiplier);
                ApplyExpScaling(enemy, finalExpMultiplier);
                ApplyDropScaling(enemy, finalDropMultiplier);

                // Y-sorting
                SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = -(int)(enemy.transform.position.y * 100);

                activeEnemies.Add(enemy);
                remainingBudget -= selectedEnemy.cost;

                float delay = Random.Range(minEnemySpawnDelay, maxEnemySpawnDelay);
                yield return new WaitForSeconds(delay);
            }
            else
            {
                spawnWeights[selectedIndex] = 0;
                totalWeight -= selectedEnemy.spawnWeight;

                if (totalWeight <= 0)
                    break;
            }
        }
    }

    private int GetWeightedRandomIndex(List<float> weights, float totalWeight)
    {
        if (totalWeight <= 0)
            return -1;

        float randomValue = Random.Range(0, totalWeight);
        float weightSum = 0;

        for (int i = 0; i < weights.Count; i++)
        {
            weightSum += weights[i];
            if (randomValue < weightSum)
                return i;
        }

        return weights.Count - 1;
    }

    private Vector3 GetSpawnPositionOffScreen(SpawnDirection direction, bool isBoss)
    {
        if (mainCamera == null)
        {
            return spawnPosition + Random.insideUnitSphere * spawnRadius;
        }

        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;

        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 spawnPos = cameraPos;

        switch (direction)
        {
            case SpawnDirection.Right:
                spawnPos.x = cameraPos.x + (width / 2) + spawnOffsetFromScreen;
                if (isBoss)
                    spawnPos.y = cameraPos.y;
                else
                    spawnPos.y = cameraPos.y + Random.Range(-spawnVerticalRandom, spawnVerticalRandom);
                break;

            case SpawnDirection.Left:
                spawnPos.x = cameraPos.x - (width / 2) - spawnOffsetFromScreen;
                spawnPos.y = cameraPos.y + Random.Range(-spawnVerticalRandom, spawnVerticalRandom);
                break;

            case SpawnDirection.Top:
                spawnPos.y = cameraPos.y + (height / 2) + spawnOffsetFromScreen;
                spawnPos.x = cameraPos.x + Random.Range(-(width / 2), width / 2);
                break;

            case SpawnDirection.Bottom:
                spawnPos.y = cameraPos.y - (height / 2) - spawnOffsetFromScreen;
                spawnPos.x = cameraPos.x + Random.Range(-(width / 2), width / 2);
                break;
        }

        spawnPos.z = transform.position.z;

        return spawnPos;
    }

    private void ApplyStatScaling(GameObject enemy, EnemyType enemyType, bool isElite)
    {
        EnemyStats enemyStats = enemy.GetComponent<EnemyStats>();

        if (enemyStats == null)
        {
            Debug.LogWarning($"Enemy {enemy.name} doesn't have an EnemyStats component!");
            return;
        }

        // ========================================
        // EXPONENTIAL GROWTH FORMULA
        // Stats = Base × (GrowthRate ^ (Wave - 1))
        // ========================================
        double waveMultiplier = System.Math.Pow(enemyGrowthRate, currentWave - 1);

        double finalHealth = baseEnemyHealth * waveMultiplier;
        double finalAttack = baseEnemyAttack * waveMultiplier;
        double finalDefense = baseEnemyDefense * waveMultiplier;

        // Apply faction modifiers
        double factionHealthMult = 1.0;
        double factionAttackMult = 1.0;

        switch (enemyType.faction)
        {
            case EnemyFaction.Swarm:
                factionHealthMult = 0.6;
                factionAttackMult = 0.7;
                break;

            case EnemyFaction.Elite:
                factionHealthMult = 2.0;
                factionAttackMult = 1.8;
                break;

            case EnemyFaction.Miniboss:
                factionHealthMult = 3.5;
                factionAttackMult = 2.5;
                break;

            case EnemyFaction.Support:
                factionHealthMult = 0.7;
                factionAttackMult = 0.6;
                break;
        }

        finalHealth *= factionHealthMult;
        finalAttack *= factionAttackMult;
        finalDefense *= factionHealthMult;

        // Apply elite status
        if (isElite)
        {
            finalHealth *= eliteStatMultiplier;
            finalAttack *= eliteStatMultiplier;
            finalDefense *= eliteStatMultiplier;

            // Visual indicator
            SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.8f, 0.3f);
            }

            if (showDetailedLogs)
                Debug.Log($"Elite {enemyType.enemyName} spawned! HP: {BigNumberFormatter.Format(finalHealth)}");
        }

        // Apply stats
        enemyStats.maxHealth = (float)finalHealth;
        enemyStats.currentHealth = (float)finalHealth;
        enemyStats.attack = (float)finalAttack;
        enemyStats.defense = (float)finalDefense;

        // Debug log every 50 waves
        if (showDetailedLogs && currentWave % 50 == 0 && !isElite)
        {
            Debug.Log($"=== WAVE {currentWave} ===");
            Debug.Log($"Enemy HP: {BigNumberFormatter.Format(finalHealth)}");
            Debug.Log($"Enemy ATK: {BigNumberFormatter.Format(finalAttack)}");
            Debug.Log($"Growth: {enemyGrowthRate}^{currentWave - 1} = {waveMultiplier:E2}");
        }
    }

    private void ApplyGoldScaling(GameObject enemy, float goldMultiplier)
    {
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            Debug.LogWarning("Enemy component not found!");
            return;
        }

        // Exponential gold growth
        double goldMult = System.Math.Pow(goldGrowthRate, currentWave - 1) * goldMultiplier;
        double goldAmount = baseGoldReward * goldMult;

        // Random variation ±10%
        goldAmount *= UnityEngine.Random.Range(0.9f, 1.1f);

        enemyComponent.gold = goldAmount;

        if (showDetailedLogs && currentWave % 50 == 0)
        {
            Debug.Log($"Wave {currentWave} Gold: {BigNumberFormatter.Format(goldAmount)}");
        }
    }

    private void ApplyExpScaling(GameObject enemy, float expMultiplier)
    {
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            Debug.LogWarning("Enemy component not found!");
            return;
        }

        // Exponential exp growth
        double expMult = System.Math.Pow(expGrowthRate, currentWave - 1);
        double expAmount = baseExpReward * expMult;

        // Random variation ±10%
        expAmount *= UnityEngine.Random.Range(0.9f, 1.1f);

        enemyComponent.exp = expAmount;

        if (showDetailedLogs && currentWave % 50 == 0)
        {
            Debug.Log($"Wave {currentWave} Exp: {BigNumberFormatter.Format(expAmount)}");
        }
    }

    private void ApplyDropScaling(GameObject enemy, float dropMultiplier)
    {
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            Debug.LogWarning("Enemy component not found!");
            return;
        }

        // Slower growth for drops
        double dropMult = System.Math.Pow(dropGrowthRate, (currentWave - 1) * 0.5);

        enemyComponent.dropMultiplier = dropMult;

        if (showDetailedLogs && currentWave % 50 == 0)
        {
            Debug.Log($"Wave {currentWave} Drop Mult: {dropMult:F2}x");
        }
    }

    // Public getters
    public int GetCurrentWave() => currentWave;
    public int GetCurrentCycle() => currentCycle;
    public bool IsBossWaveActive() => isBossWave && isWaveInProgress;
    public bool IsSpecialWaveActive() => currentSpecialWave != null && isWaveInProgress;
    public string GetCurrentSpecialWaveName() => currentSpecialWave != null ? currentSpecialWave.waveName : "";

    public void NotifyEnemyDeath(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
}