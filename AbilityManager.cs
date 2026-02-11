using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class AbilityManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform invContainer;
    public GameObject container;
    public GameObject dividerContainer;
    public Transform unlockedContainer;

    [Header("Ability Database")]
    public Ability[] allAbilityData;
    public Image[] abilityRarityFrames;
    public GameObject defaultFramePrefab;

    [Header("Locked Abilities UI")]
    public GameObject dividerPrefab;
    public GameObject lockedAbilityPrefab;
    public string dividerText = "--- Locked Abilities ---";

    [Header("Equipped Abilities UI")]
    public AbilitySlot[] abilitySlots;

    [Header("Ability Management")]
    public List<OwnedAbility> ownedAbilities = new List<OwnedAbility>();
    private string[] equippedAbilities = new string[6];

    [Header("Leveling System")]
    public int baseLevelUpCost = 100;
    public float levelCostMultiplier = 1.5f;
    public int maxAbilityLevel = 10;

    public GameManager gameManager;
    private Abilities playerAbilities;

    // Track spawned UI objects for cleanup
    private readonly List<GameObject> spawnedInventoryItems = new List<GameObject>();
    private readonly List<GameObject> spawnedEquippedItems = new List<GameObject>();

    void Awake()
    {
        allAbilityData = gameManager.abilityDatabase;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerAbilities = player.GetComponent<Abilities>();
        }

        LoadAbilities();
        LoadEquippedAbilities();
    }

    private void Start()
    {
        SkillIconEffect.ForceCleanupAllAnimations();

        ForceInitializePlayerAbilities();
        StartCoroutine(AutoEquipSavedAbilities());
        CreateAbilitiesList();
        InitializeEquippedAbilitiesUI();
        StartCoroutine(EnsurePlayerAbilitiesLoaded());
    }

    private void OnDestroy()
    {
        CleanupAllSpawnedObjects();
        SkillIconEffect.ForceCleanupAllAnimations();
    }

    #region Memory Management
    private void CleanupAllSpawnedObjects()
    {
        foreach (var item in spawnedInventoryItems)
        {
            if (item != null)
                Destroy(item);
        }
        spawnedInventoryItems.Clear();

        foreach (var item in spawnedEquippedItems)
        {
            if (item != null)
                Destroy(item);
        }
        spawnedEquippedItems.Clear();

        if (invContainer != null)
        {
            foreach (Transform child in invContainer)
            {
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
    }

    private void RegisterInventoryItem(GameObject item)
    {
        if (item != null && !spawnedInventoryItems.Contains(item))
        {
            spawnedInventoryItems.Add(item);
        }
    }

    private void RegisterEquippedItem(GameObject item)
    {
        if (item != null && !spawnedEquippedItems.Contains(item))
        {
            spawnedEquippedItems.Add(item);
        }
    }
    #endregion

    private IEnumerator EnsurePlayerAbilitiesLoaded()
    {
        yield return new WaitUntil(() => GameObject.FindWithTag("Player") != null);

        GameObject player = GameObject.FindWithTag("Player");
        Abilities abilities = player.GetComponent<Abilities>();

        if (abilities != null)
        {
            yield return new WaitForSeconds(0.1f);

            abilities.abilities.Clear();
            for (int i = 0; i < 6; i++)
            {
                abilities.abilities.Add(null);
            }

            for (int i = 0; i < 6; i++)
            {
                if (!string.IsNullOrEmpty(equippedAbilities[i]))
                {
                    Ability actualAbility = GetAbilityData(equippedAbilities[i]);
                    if (actualAbility != null)
                    {
                        abilities.abilities[i] = actualAbility;
                    }
                }
            }

            abilities.OnAbilitiesChanged();
        }

        gameObject.SetActive(false);
    }

    private void ForceInitializePlayerAbilities()
    {
        if (playerAbilities == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerAbilities = player.GetComponent<Abilities>();
            }
        }

        if (playerAbilities != null)
        {
            playerAbilities.abilities.Clear();
            for (int i = 0; i < 6; i++)
            {
                playerAbilities.abilities.Add(null);
            }
        }
    }

    private IEnumerator AutoEquipSavedAbilities()
    {
        yield return new WaitForEndOfFrame();

        SkillIconEffect.ForceCleanupAllAnimations();

        for (int i = 0; i < 6; i++)
        {
            if (!string.IsNullOrEmpty(equippedAbilities[i]))
            {
                ApplyAbilityToPlayer(equippedAbilities[i], i);
            }
        }

        if (playerAbilities != null)
        {
            playerAbilities.OnAbilitiesChanged();
        }

        CooldownHandler cooldownHandler = FindFirstObjectByType<CooldownHandler>();
        if (cooldownHandler != null)
        {
            cooldownHandler.UpdateAbilityUI();
        }
    }

    #region Save/Load System
    void LoadAbilities()
    {
        string savedData = PlayerPrefs.GetString("Abilities", "");
        if (string.IsNullOrEmpty(savedData))
        {
            return;
        }
        string[] allAbilities = savedData.Split(';');
        ownedAbilities.Clear();
        foreach (string oneAbility in allAbilities)
        {
            if (string.IsNullOrEmpty(oneAbility)) continue;
            string[] abilityInfo = oneAbility.Split(',');
            if (abilityInfo.Length >= 2)
            {
                OwnedAbility ownedAbility = new OwnedAbility
                {
                    abilityName = abilityInfo[0],
                    level = int.Parse(abilityInfo[1])
                };
                ownedAbilities.Add(ownedAbility);
            }
        }
    }

    void LoadEquippedAbilities()
    {
        for (int i = 0; i < 6; i++)
        {
            equippedAbilities[i] = PlayerPrefs.GetString($"EquippedAbility_{i}", "");
        }
    }

    void SaveAbilities()
    {
        string dataToSave = string.Join(";", ownedAbilities.Select(ability => $"{ability.abilityName},{ability.level}"));
        PlayerPrefs.SetString("Abilities", dataToSave);
        PlayerPrefs.Save();
    }

    void SaveEquippedAbilities()
    {
        for (int i = 0; i < 6; i++)
        {
            PlayerPrefs.SetString($"EquippedAbility_{i}", equippedAbilities[i] ?? "");
        }
        PlayerPrefs.Save();
    }
    #endregion

    #region Ability Equipment System
    public bool EquipAbility(string abilityName, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 6) return false;
        if (!IsAbilityOwned(abilityName)) return false;

        if (playerAbilities == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerAbilities = player.GetComponent<Abilities>();
            }
        }

        if (playerAbilities == null)
        {
            return false;
        }

        AbilityDragDrop[] allDragDrops = FindObjectsByType<AbilityDragDrop>(FindObjectsSortMode.None);
        foreach (var dragDrop in allDragDrops)
        {
            if (dragDrop.abilityName == abilityName && (dragDrop.isDragging || dragDrop.CurrentSlot == null))
            {
                Destroy(dragDrop.gameObject);
                break;
            }
        }

        for (int i = 0; i < 6; i++)
        {
            if (i != slotIndex && equippedAbilities[i] == abilityName)
            {
                UnequipAbility(i);
                break;
            }
        }

        if (!string.IsNullOrEmpty(equippedAbilities[slotIndex]))
        {
            UnequipAbility(slotIndex);
        }

        Ability actualAbility = GetAbilityData(abilityName);
        if (actualAbility == null)
        {
            return false;
        }

        while (playerAbilities.abilities.Count <= slotIndex)
        {
            playerAbilities.abilities.Add(null);
        }

        playerAbilities.abilities[slotIndex] = actualAbility;

        if (playerAbilities.abilities[slotIndex] != null)
        {
            float cooldownTime = actualAbility.cooldownTime;

            while (playerAbilities.abilityCooldowns.Count <= slotIndex)
            {
                playerAbilities.abilityCooldowns.Add(0f);
            }
            while (playerAbilities.abilityCooldownTimes.Count <= slotIndex)
            {
                playerAbilities.abilityCooldownTimes.Add(0f);
            }

            playerAbilities.abilityCooldowns[slotIndex] = cooldownTime;
            playerAbilities.abilityCooldownTimes[slotIndex] = cooldownTime;

            if (playerAbilities.cooldownUI != null && slotIndex < playerAbilities.cooldownUI.Count && playerAbilities.cooldownUI[slotIndex] != null)
            {
                playerAbilities.cooldownUI[slotIndex].SetMaxCooldown(cooldownTime);
                playerAbilities.cooldownUI[slotIndex].SetCooldown(cooldownTime);
            }
        }
        else
        {
            return false;
        }

        equippedAbilities[slotIndex] = abilityName;
        SaveEquippedAbilities();

        CreateEquippedAbilityUI(abilityName, slotIndex);

        playerAbilities.OnAbilitiesChanged();

        CooldownHandler cooldownHandler = FindFirstObjectByType<CooldownHandler>();
        if (cooldownHandler != null)
        {
            cooldownHandler.UpdateAbilityUI();
        }

        CreateAbilitiesList();

        return true;
    }

    public void UnequipAbility(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 6)
        {
            return;
        }

        string previousAbility = equippedAbilities[slotIndex];
        if (string.IsNullOrEmpty(previousAbility))
        {
            return;
        }

        equippedAbilities[slotIndex] = "";
        SaveEquippedAbilities();
        RemoveAbilityFromPlayer(slotIndex);

        if (abilitySlots != null && slotIndex < abilitySlots.Length && abilitySlots[slotIndex] != null)
        {
            abilitySlots[slotIndex].RemoveItem();
        }
        CreateAbilitiesList();
    }

    private void ApplyAbilityToPlayer(string abilityName, int slotIndex)
    {
        if (playerAbilities == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerAbilities = player.GetComponent<Abilities>();
            }
        }

        if (playerAbilities == null)
        {
            return;
        }

        Ability abilityData = GetAbilityData(abilityName);
        if (abilityData == null)
        {
            return;
        }

        while (playerAbilities.abilities.Count <= slotIndex)
        {
            playerAbilities.abilities.Add(null);
        }

        playerAbilities.abilities[slotIndex] = abilityData;

        playerAbilities.OnAbilitiesChanged();
    }

    private IEnumerator DelayedAbilityNotification()
    {
        yield return new WaitForEndOfFrame();

        if (playerAbilities != null)
        {
            playerAbilities.OnAbilitiesChanged();
        }

        CooldownHandler cooldownHandler = FindFirstObjectByType<CooldownHandler>();
        if (cooldownHandler != null)
        {
            cooldownHandler.UpdateAbilityUI();
        }
    }

    private void RemoveAbilityFromPlayer(int slotIndex)
    {
        if (playerAbilities == null) return;

        if (slotIndex < playerAbilities.abilities.Count)
        {
            playerAbilities.abilities[slotIndex] = null;

            playerAbilities.OnAbilitiesChanged();

            CooldownHandler cooldownHandler = FindFirstObjectByType<CooldownHandler>();
            if (cooldownHandler != null)
            {
                cooldownHandler.UpdateAbilityUI();
            }
        }
    }

    public void InitializeEquippedAbilitiesUI()
    {
        if (abilitySlots == null)
        {
            return;
        }

        foreach (var item in spawnedEquippedItems.ToList())
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedEquippedItems.Clear();

        for (int i = 0; i < abilitySlots.Length && i < 6; i++)
        {
            if (abilitySlots[i] == null)
            {
                continue;
            }

            abilitySlots[i].RemoveItem();

            GameObject abilityUI = Instantiate(defaultFramePrefab, abilitySlots[i].transform);
            abilitySlots[i].currentItem = abilityUI;
            RegisterEquippedItem(abilityUI);

            Transform abilityImageTransform = abilityUI.transform.Find("Ability Image");
            if (abilityImageTransform != null)
            {
                abilityImageTransform.gameObject.SetActive(false);
            }

            if (!string.IsNullOrEmpty(equippedAbilities[i]))
            {
                CreateEquippedAbilityUI(equippedAbilities[i], i);
            }
        }
    }

    public void InitializeEquippedAbilitiesInGame()
    {
        if (playerAbilities == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerAbilities = player.GetComponent<Abilities>();
            }
            if (playerAbilities == null) return;
        }

        playerAbilities.abilities.Clear();
        for (int i = 0; i < 6; i++)
        {
            playerAbilities.abilities.Add(null);
        }

        for (int i = 0; i < 6; i++)
        {
            if (!string.IsNullOrEmpty(equippedAbilities[i]))
            {
                Ability actualAbility = GetAbilityData(equippedAbilities[i]);
                if (actualAbility != null)
                {
                    playerAbilities.abilities[i] = actualAbility;
                }
            }
        }

        playerAbilities.OnAbilitiesChanged();
    }

    private void CreateEquippedAbilityUI(string abilityName, int slotIndex)
    {
        if (abilitySlots == null || slotIndex >= abilitySlots.Length || abilitySlots[slotIndex] == null)
        {
            return;
        }

        Ability abilityData = GetAbilityData(abilityName);
        if (abilityData == null)
        {
            return;
        }

        abilitySlots[slotIndex].RemoveItem();
        GameObject abilityUI = Instantiate(defaultFramePrefab, abilitySlots[slotIndex].transform);
        RegisterEquippedItem(abilityUI);

        SetAbilityFrame(abilityUI, abilityData.rarity);

        Image frameImage = abilityUI.GetComponent<Image>();
        if (frameImage != null)
        {
            Color frameColor = frameImage.color;
            frameColor.a = 1.0f;
            frameImage.color = frameColor;
        }

        AbilityDragDrop dragDrop = abilityUI.GetComponent<AbilityDragDrop>();
        if (dragDrop != null)
        {
            dragDrop.canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            dragDrop.abilityName = abilityName;
            dragDrop.SetCurrentSlot(abilitySlots[slotIndex]);
        }

        Transform abilityImageTransform = abilityUI.transform.Find("Ability Image");
        if (abilityImageTransform != null)
        {
            Image abilityImage = abilityImageTransform.GetComponent<Image>();
            if (abilityImage != null && abilityData.image != null)
            {
                abilityImage.sprite = abilityData.image;
                abilityImage.color = Color.white;
            }
        }

        abilitySlots[slotIndex].currentItem = abilityUI;

        CooldownHandler cooldownHandler = FindFirstObjectByType<CooldownHandler>();
        if (cooldownHandler != null)
        {
            cooldownHandler.UpdateAbilityUI();
        }
    }
    #endregion

    #region Ability Inventory Management
    public void AddAbility(string abilityName)
    {
        Ability abilityData = GetAbilityData(abilityName);
        if (abilityData == null)
        {
            return;
        }

        if (IsAbilityOwned(abilityName))
        {
            return;
        }

        OwnedAbility newOwnedAbility = new OwnedAbility
        {
            abilityName = abilityName,
            level = 1
        };
        ownedAbilities.Add(newOwnedAbility);
        SaveAbilities();
        CreateAbilitiesList();
    }

    public void RemoveAbility(string abilityName)
    {
        for (int i = 0; i < 6; i++)
        {
            if (equippedAbilities[i] == abilityName)
            {
                UnequipAbility(i);
            }
        }

        int removedCount = ownedAbilities.RemoveAll(ability => ability.abilityName == abilityName);
        if (removedCount > 0)
        {
            SaveAbilities();
            CreateAbilitiesList();
        }
    }

    public bool UpgradeAbility(string abilityName)
    {
        OwnedAbility abilityToUpgrade = ownedAbilities.FirstOrDefault(ability => ability.abilityName == abilityName);
        if (abilityToUpgrade == null)
        {
            return false;
        }

        if (abilityToUpgrade.level >= maxAbilityLevel)
        {
            return false;
        }

        int upgradeCost = GetUpgradeCost(abilityToUpgrade.level);
        int currentCurrency = PlayerPrefs.GetInt("Currency", 0);

        if (currentCurrency < upgradeCost)
        {
            return false;
        }

        PlayerPrefs.SetInt("Currency", currentCurrency - upgradeCost);

        abilityToUpgrade.level++;
        SaveAbilities();
        UpdateEquippedAbilityStats(abilityName);
        CreateAbilitiesList();

        return true;
    }

    public bool UpgradeAbilityWithDuplicate(string abilityName)
    {
        var duplicates = ownedAbilities.Where(a => a.abilityName == abilityName).ToList();
        if (duplicates.Count < 2)
        {
            return false;
        }

        OwnedAbility mainAbility = duplicates.OrderByDescending(a => a.level).First();
        if (mainAbility.level >= maxAbilityLevel)
        {
            return false;
        }

        ownedAbilities.Remove(duplicates[1]);

        mainAbility.level++;
        SaveAbilities();
        UpdateEquippedAbilityStats(abilityName);
        CreateAbilitiesList();

        return true;
    }

    public int GetUpgradeCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseLevelUpCost * Mathf.Pow(levelCostMultiplier, currentLevel - 1));
    }

    private void UpdateEquippedAbilityStats(string abilityName)
    {
        for (int i = 0; i < 6; i++)
        {
            if (equippedAbilities[i] == abilityName)
            {
                ApplyAbilityToPlayer(abilityName, i);
            }
        }
    }
    #endregion

    #region Data Access Methods
    public Ability GetAbilityData(string abilityName)
    {
        return allAbilityData.FirstOrDefault(data => data.name == abilityName);
    }

    public OwnedAbility GetOwnedAbility(string abilityName)
    {
        return ownedAbilities.FirstOrDefault(ability => ability.abilityName == abilityName);
    }

    public string GetEquippedAbility(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 6)
            return equippedAbilities[slotIndex] ?? "";
        return "";
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 6)
            return string.IsNullOrEmpty(equippedAbilities[slotIndex]);
        return true;
    }

    public bool IsAbilityOwned(string abilityName)
    {
        return ownedAbilities.Any(ability => ability.abilityName == abilityName);
    }

    public bool IsAbilityEquipped(string abilityName)
    {
        return equippedAbilities.Contains(abilityName);
    }

    public List<OwnedAbility> GetOwnedAbilities()
    {
        return new List<OwnedAbility>(ownedAbilities);
    }

    public List<Ability> GetLockedAbilities()
    {
        return allAbilityData.Where(abilityData => !IsAbilityOwned(abilityData.name)).ToList();
    }
    #endregion

    #region UI Creation
    public void CreateAbilitiesList()
    {
        foreach (var item in spawnedInventoryItems.ToList())
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedInventoryItems.Clear();

        foreach (Transform child in invContainer)
        {
            Destroy(child.gameObject);
        }

        CreateUnequippedAbilitiesSection();

        List<Ability> lockedAbilities = GetLockedAbilities();
        if (lockedAbilities.Count > 0)
        {
            CreateDivider();
            CreateLockedAbilitiesSection(lockedAbilities);
        }
    }

    private void CreateUnequippedAbilitiesSection()
    {
        List<OwnedAbility> unequippedAbilities = ownedAbilities.Where(ability => !IsAbilityEquipped(ability.abilityName)).ToList();
        if (unequippedAbilities.Count == 0)
        {
            return;
        }

        GameObject unlockedParent = Instantiate(container, invContainer);
        RegisterInventoryItem(unlockedParent);
        unlockedContainer = unlockedParent.transform;

        foreach (OwnedAbility ability in unequippedAbilities)
        {
            CreateOwnedAbilityUI(ability, unlockedParent.transform);
        }
    }

    private void CreateLockedAbilitiesSection(List<Ability> lockedAbilities)
    {
        GameObject lockedParent = Instantiate(container, invContainer);
        RegisterInventoryItem(lockedParent);

        foreach (Ability lockedAbility in lockedAbilities)
        {
            CreateLockedAbilityUI(lockedAbility, lockedParent.transform);
        }
    }

    private void CreateOwnedAbilityUI(OwnedAbility ability, Transform parent)
    {
        Ability abilityData = GetAbilityData(ability.abilityName);
        if (abilityData == null)
        {
            return;
        }

        GameObject abilityUI = Instantiate(defaultFramePrefab, parent);
        RegisterInventoryItem(abilityUI);

        SetAbilityFrame(abilityUI, abilityData.rarity);

        AbilityDragDrop dragDrop = abilityUI.GetComponent<AbilityDragDrop>();
        if (dragDrop != null)
        {
            dragDrop.canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            dragDrop.abilityName = ability.abilityName;
            dragDrop.SetCurrentSlot(null);
        }

        Transform abilityImageTransform = abilityUI.transform.Find("Ability Image");
        if (abilityImageTransform != null)
        {
            Image abilityImage = abilityImageTransform.GetComponent<Image>();
            if (abilityImage != null && abilityData.image != null)
            {
                abilityImage.sprite = abilityData.image;
                abilityImage.color = Color.white;
            }
        }

        Transform levelTextTransform = abilityUI.transform.Find("Level Text");
        if (levelTextTransform != null)
        {
            Text levelText = levelTextTransform.GetComponent<Text>();
            if (levelText != null)
            {
                levelText.text = ability.level > 1 ? $"Lv.{ability.level}" : "";
            }
        }

        Button upgradeButton = abilityUI.GetComponentInChildren<Button>();
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => UpgradeAbility(ability.abilityName));
        }
    }

    private void CreateLockedAbilityUI(Ability abilityData, Transform parent)
    {
        GameObject prefabToUse = lockedAbilityPrefab != null ? lockedAbilityPrefab : defaultFramePrefab;
        GameObject abilityUI = Instantiate(prefabToUse, parent);
        RegisterInventoryItem(abilityUI);

        AbilityDragDrop dragDrop = abilityUI.GetComponent<AbilityDragDrop>();
        if (dragDrop != null)
        {
            dragDrop.enabled = false;
        }

        SetAbilityFrame(abilityUI, abilityData.rarity);

        Transform abilityImageTransform = abilityUI.transform.Find("Ability Image");
        if (abilityImageTransform != null)
        {
            Image abilityImage = abilityImageTransform.GetComponent<Image>();
            if (abilityImage != null && abilityData.image != null)
            {
                abilityImage.sprite = abilityData.image;
                abilityImage.color = Color.gray;
            }
        }

        Image frameImage = abilityUI.GetComponent<Image>();
        if (frameImage != null)
        {
            Color frameColor = frameImage.color;
            frameColor.a = 0.5f;
            frameImage.color = frameColor;
        }
    }

    private void CreateDivider()
    {
        if (dividerPrefab == null || dividerContainer == null) return;

        GameObject parent = Instantiate(dividerContainer, invContainer);
        RegisterInventoryItem(parent);
        GameObject divider = Instantiate(dividerPrefab, parent.transform);

        Text dividerTextComponent = divider.GetComponent<Text>();
        if (dividerTextComponent != null)
        {
            dividerTextComponent.text = dividerText;
        }

        RectTransform parentRect = parent.GetComponent<RectTransform>();
        if (parentRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    private void SetAbilityFrame(GameObject abilityUI, Rarity rarity)
    {
        if (abilityRarityFrames == null || abilityRarityFrames.Length == 0) return;

        Image frameImage = abilityUI.GetComponent<Image>();
        if (frameImage == null) return;

        int rarityIndex = (int)rarity;
        if (rarityIndex >= 0 && rarityIndex < abilityRarityFrames.Length && abilityRarityFrames[rarityIndex] != null)
        {
            frameImage.sprite = abilityRarityFrames[rarityIndex].sprite;
            frameImage.color = abilityRarityFrames[rarityIndex].color;
            frameImage.material = abilityRarityFrames[rarityIndex].material;
        }
    }
    #endregion
}

[System.Serializable]
public class OwnedAbility
{
    public string abilityName;
    public int level;
}
