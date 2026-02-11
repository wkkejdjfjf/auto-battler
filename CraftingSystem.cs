using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Main crafting system manager
public class CraftingSystem : MonoBehaviour
{
    // Singleton pattern for easy access
    public static CraftingSystem Instance { get; private set; }

    // Lists for resources and craftable items that can be populated in the inspector
    public List<ResourceSO> availableResources = new List<ResourceSO>();
    public List<CraftableItemSO> craftableItems = new List<CraftableItemSO>();

    // Master lists of all potential resources and items in the game
    public List<ResourceSO> allResources = new List<ResourceSO>();
    public List<CraftableItemSO> allItems = new List<CraftableItemSO>();

    // Lists for items that are initially unlocked (available from start)
    public List<ResourceSO> initialResources = new List<ResourceSO>();
    public List<CraftableItemSO> initialItems = new List<CraftableItemSO>();

    // Dictionary to track resource inventory
    private Dictionary<string, int> resourceInventory = new Dictionary<string, int>();

    // Dictionary to track crafted items
    public Dictionary<string, ItemData> craftedItemsData = new Dictionary<string, ItemData>();

    // Keys for PlayerPrefs
    private const string AVAILABLE_RESOURCES_KEY = "AvailableResources";
    private const string AVAILABLE_ITEMS_KEY = "AvailableItems";

    // Event to notify UI when inventory changes
    public event Action OnInventoryChanged;
    public event Action OnAvailableItemsChanged;
    public event Action<string> OnItemUnlocked;
    public event Action<string> OnResourceUnlocked;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Clear initial lists if they were populated in the inspector
        // We'll rebuild them from saved data
        availableResources.Clear();
        craftableItems.Clear();

        // Load available resources and items from PlayerPrefs
        LoadAvailableResources();
        LoadAvailableItems();

        // If this is the first run, add initial resources and items
        if (string.IsNullOrEmpty(PlayerPrefs.GetString(AVAILABLE_RESOURCES_KEY, "")))
        {
            foreach (var resource in initialResources)
            {
                UnlockResource(resource);
            }
        }

        if (string.IsNullOrEmpty(PlayerPrefs.GetString(AVAILABLE_ITEMS_KEY, "")))
        {
            foreach (var item in initialItems)
            {
                UnlockCraftableItem(item);
            }
        }

        // Initialize inventory dictionaries with quantities
        InitializeInventory();

        // Check for unlockable items
        CheckForUnlockableItems();
    }

    // Load available resources from PlayerPrefs
    private void LoadAvailableResources()
    {
        string savedResourcesString = PlayerPrefs.GetString(AVAILABLE_RESOURCES_KEY, "");

        if (!string.IsNullOrEmpty(savedResourcesString))
        {
            string[] resourceIds = savedResourcesString.Split(',');

            foreach (string resourceId in resourceIds)
            {
                if (string.IsNullOrEmpty(resourceId))
                    continue;

                // Find the resource in allResources
                ResourceSO resource = allResources.FirstOrDefault(r => r.resourceId == resourceId);

                if (resource != null)
                {
                    availableResources.Add(resource);
                }
                else
                {
                    Debug.LogWarning($"Could not find resource with ID {resourceId} in allResources list");
                }
            }
        }
        else
        {
            // If no saved data, use whatever was set in the inspector
            // This only happens on first run
            Debug.Log("No saved resources found, using initial resources from inspector");
        }
    }

    // Load available items from PlayerPrefs
    private void LoadAvailableItems()
    {
        string savedItemsString = PlayerPrefs.GetString(AVAILABLE_ITEMS_KEY, "");

        if (!string.IsNullOrEmpty(savedItemsString))
        {
            string[] itemIds = savedItemsString.Split(',');

            foreach (string itemId in itemIds)
            {
                if (string.IsNullOrEmpty(itemId))
                    continue;

                // Find the item in allItems
                CraftableItemSO item = allItems.FirstOrDefault(i => i.itemId == itemId);

                if (item != null)
                {
                    craftableItems.Add(item);
                }
                else
                {
                    Debug.LogWarning($"Could not find item with ID {itemId} in allItems list");
                }
            }
        }
        else
        {
            // If no saved data, use whatever was set in the inspector
            // This only happens on first run
            Debug.Log("No saved items found, using initial items from inspector");
        }
    }

    // Save available resources to PlayerPrefs
    private void SaveAvailableResources()
    {
        string resourceIdsString = string.Join(",", availableResources.Select(r => r.resourceId));
        PlayerPrefs.SetString(AVAILABLE_RESOURCES_KEY, resourceIdsString);
        PlayerPrefs.Save();
    }

    // Save available items to PlayerPrefs
    private void SaveAvailableItems()
    {
        string itemIdsString = string.Join(",", craftableItems.Select(i => i.itemId));
        PlayerPrefs.SetString(AVAILABLE_ITEMS_KEY, itemIdsString);
        PlayerPrefs.Save();
    }

    // Initialize the inventory and load saved quantities
    public void InitializeInventory()
    {
        // Clear existing inventory data
        resourceInventory.Clear();
        craftedItemsData.Clear();

        // Initialize resource inventory with saved quantities (ensure non-negative)
        foreach (var resource in availableResources)
        {
            int savedAmount = PlayerPrefs.GetInt("Resource_" + resource.resourceId, 0);
            resourceInventory[resource.resourceId] = Mathf.Max(0, savedAmount);
        }

        // Initialize crafted items with saved quantities and level data (ensure non-negative)
        foreach (var item in craftableItems)
        {
            ItemData data = new ItemData();
            data.count = Mathf.Max(0, PlayerPrefs.GetInt("CraftedItem_" + item.itemId, 0));
            data.level = Mathf.Max(1, PlayerPrefs.GetInt("ItemLevel_" + item.itemId, 1));

            // Calculate copies needed for next level
            float levelProgress = (float)data.level / item.maxLevel;
            data.copiesForNextLevel = Mathf.Max(0, Mathf.RoundToInt(item.levelingCurve.Evaluate(levelProgress)));

            craftedItemsData[item.itemId] = data;
        }

        // Notify UI
        OnInventoryChanged?.Invoke();
    }

    // Add a resource to available resources
    public void UnlockResource(ResourceSO resource)
    {
        if (!availableResources.Any(r => r.resourceId == resource.resourceId))
        {
            availableResources.Add(resource);

            // Initialize inventory entry
            if (!resourceInventory.ContainsKey(resource.resourceId))
            {
                resourceInventory.Add(resource.resourceId, 0);
            }

            // Save the updated list of available resources
            SaveAvailableResources();

            // Notify UI
            OnAvailableItemsChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            OnResourceUnlocked?.Invoke(resource.resourceId);

            Debug.Log($"Unlocked new resource: {resource.resourceId}");
        }
    }

    // Add a craftable item to available items
    public void UnlockCraftableItem(CraftableItemSO item)
    {
        if (!craftableItems.Any(i => i.itemId == item.itemId))
        {
            craftableItems.Add(item);

            // Initialize item data
            if (!craftedItemsData.ContainsKey(item.itemId))
            {
                ItemData data = new ItemData();
                data.count = 0;
                data.level = 1;

                // Calculate copies needed for next level
                float levelProgress = (float)data.level / item.maxLevel;
                data.copiesForNextLevel = Mathf.Max(0, Mathf.RoundToInt(item.levelingCurve.Evaluate(levelProgress)));

                craftedItemsData[item.itemId] = data;
            }

            // Save the updated list of available items
            SaveAvailableItems();

            // Notify UI
            OnAvailableItemsChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            OnItemUnlocked?.Invoke(item.itemId);

            Debug.Log($"Unlocked new craftable item: {item.itemId}");
        }
    }

    // Add resources to inventory (only accepts positive amounts)
    public void AddResource(string resourceId, int amount)
    {
        // Ignore negative amounts - AddResource should only add, not remove
        if (amount <= 0)
        {
            Debug.LogWarning($"AddResource called with non-positive amount ({amount}) for {resourceId}. Use RemoveResource or SetResourceAmount instead.");
            return;
        }

        if (resourceInventory.ContainsKey(resourceId))
        {
            resourceInventory[resourceId] = resourceInventory[resourceId] + amount;
            SaveResourceData();
            OnInventoryChanged?.Invoke();

            // Check if any items can now be unlocked
            CheckForUnlockableItems();
        }
        else
        {
            // Try to find and unlock the resource first
            ResourceSO resource = allResources.FirstOrDefault(r => r.resourceId == resourceId);
            if (resource != null)
            {
                UnlockResource(resource);
                // Now that it's unlocked, add the amount
                resourceInventory[resourceId] = resourceInventory[resourceId] + amount;
                SaveResourceData();
                OnInventoryChanged?.Invoke();

                // Check if any items can now be unlocked
                CheckForUnlockableItems();
            }
            else
            {
                Debug.LogWarning($"Failed to add resource: {resourceId} not found in allResources list");
            }
        }
    }

    // Remove resources from inventory
    public void RemoveResource(string resourceId, int amount)
    {
        // Ignore negative amounts
        if (amount <= 0)
        {
            Debug.LogWarning($"RemoveResource called with non-positive amount ({amount}) for {resourceId}.");
            return;
        }

        if (resourceInventory.ContainsKey(resourceId))
        {
            // Ensure we don't go below 0
            resourceInventory[resourceId] = Mathf.Max(0, resourceInventory[resourceId] - amount);
            SaveResourceData();
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"Cannot remove resource {resourceId}: not in inventory");
        }
    }

    // Set resource amount directly (with negative protection)
    public void SetResourceAmount(string resourceId, int amount)
    {
        if (resourceInventory.ContainsKey(resourceId))
        {
            resourceInventory[resourceId] = Mathf.Max(0, amount);
            SaveResourceData();
            OnInventoryChanged?.Invoke();
            CheckForUnlockableItems();
        }
        else
        {
            Debug.LogWarning($"Cannot set amount for resource {resourceId}: not in inventory");
        }
    }

    // Get current amount of a resource
    public int GetResourceAmount(string resourceId)
    {
        if (resourceInventory.ContainsKey(resourceId))
        {
            return Mathf.Max(0, resourceInventory[resourceId]); // Extra safety
        }
        return 0;
    }

    // Get resource data by ID
    public ResourceSO GetResourceData(string resourceId)
    {
        return availableResources.FirstOrDefault(r => r.resourceId == resourceId);
    }

    // Get current amount of a crafted item
    public int GetCraftedItemAmount(string itemId)
    {
        if (craftedItemsData.ContainsKey(itemId))
        {
            return Mathf.Max(0, craftedItemsData[itemId].count); // Extra safety
        }
        return 0;
    }

    // Get craftable item data by ID
    public CraftableItemSO GetCraftableItemData(string itemId)
    {
        CraftableItemSO item = craftableItems.FirstOrDefault(i => i.itemId == itemId);
        if (item != null) return item;
        return allItems.FirstOrDefault(i => i.itemId == itemId);
    }

    // Check if player can craft an item, with an optional amount parameter (defaults to 1)
    public bool CanCraft(string itemId, int amount = 1)
    {
        // Ensure amount is positive
        if (amount <= 0)
            return false;

        CraftableItemSO item = craftableItems.FirstOrDefault(i => i.itemId == itemId);

        if (item == null)
            return false;

        foreach (var requirement in item.requirements)
        {
            string reqResourceId = requirement.resource.resourceId;
            int requiredAmount = requirement.amount * amount;

            if (!resourceInventory.ContainsKey(reqResourceId) ||
                resourceInventory[reqResourceId] < requiredAmount)
            {
                return false;
            }
        }

        return true;
    }

    // Craft an item, with an optional amount parameter (defaults to 1)
    public bool CraftItem(string itemId, int amount = 1)
    {
        // Validate amount
        if (amount <= 0)
            return false;

        // Check if we can craft the requested amount
        if (!CanCraft(itemId, amount))
            return false;

        CraftableItemSO item = craftableItems.FirstOrDefault(i => i.itemId == itemId);

        // Deduct resources based on the amount (with negative protection)
        foreach (var requirement in item.requirements)
        {
            string reqResourceId = requirement.resource.resourceId;
            int requiredAmount = requirement.amount * amount;

            // Ensure we don't go below 0 (extra safety check)
            resourceInventory[reqResourceId] = Mathf.Max(0, resourceInventory[reqResourceId] - requiredAmount);
        }

        // Add crafted items
        if (!craftedItemsData.ContainsKey(itemId))
        {
            ItemData data = new ItemData();
            data.level = 1;
            data.count = 0;

            // Calculate copies needed for next level
            float levelProgress = (float)data.level / item.maxLevel;
            data.copiesForNextLevel = Mathf.Max(0, Mathf.RoundToInt(item.levelingCurve.Evaluate(levelProgress)));

            craftedItemsData[itemId] = data;
        }

        // Add the specified amount of items (ensure non-negative)
        craftedItemsData[itemId].count = Mathf.Max(0, craftedItemsData[itemId].count + amount);

        // Save data and notify UI
        SaveResourceData();
        OnInventoryChanged?.Invoke();

        // Check if any items can now be unlocked due to crafting requirement
        CheckForUnlockableItems();

        return true;
    }


    /// <summary>
    /// Craft a random weapon from a rarity tier using fragments
    /// </summary>
    public CraftableItemSO CraftRandomWeapon(Rarity rarity)
    {
        // Get fragment cost and ID
        int fragmentCost = GetFragmentCostForRarity(rarity);
        string fragmentId = GetFragmentIdForRarity(rarity);

        // Check if player has enough fragments
        if (GetResourceAmount(fragmentId) < fragmentCost)
        {
            Debug.Log($"Not enough {fragmentId}. Need {fragmentCost}, have {GetResourceAmount(fragmentId)}");
            return null;
        }

        // Get weapon pool for this rarity (only uncrafted weapons)
        List<CraftableItemSO> weaponPool = GetUncraftedWeaponsOfRarity(rarity);

        // If no uncrafted weapons, try getting all weapons (for duplicate handling)
        if (weaponPool.Count == 0)
        {
            weaponPool = GetAllWeaponsOfRarity(rarity);

            if (weaponPool.Count == 0)
            {
                Debug.Log($"No {rarity} weapons exist in the game!");
                return null;
            }

            Debug.Log($"All {rarity} weapons collected! Rolling for duplicates...");
        }

        // Pick random weapon from pool
        CraftableItemSO randomWeapon = weaponPool[UnityEngine.Random.Range(0, weaponPool.Count)];

        // Consume fragments
        RemoveResource(fragmentId, fragmentCost);

        // Check if this is a duplicate
        bool isDuplicate = craftedItemsData.ContainsKey(randomWeapon.itemId);

        if (isDuplicate)
        {
            // Duplicate crafted - convert to Soul Essence
            int essenceReward = GetDuplicateConversionAmount(rarity);
            AddResource("SoulEssence", essenceReward);

            Debug.Log($"Duplicate {randomWeapon.displayName}! Converted to {essenceReward} Soul Essence");
        }
        else
        {
            // New weapon - add to inventory
            ItemData data = new ItemData();
            data.level = 1;
            data.count = 1;
            craftedItemsData[randomWeapon.itemId] = data;

            Debug.Log($"✨ Crafted NEW weapon: {randomWeapon.displayName}! ✨");
        }

        SaveResourceData();
        OnInventoryChanged?.Invoke();

        return randomWeapon;
    }

    /// <summary>
    /// Craft multiple weapons at once (for "Craft x10" feature)
    /// </summary>
    public List<CraftableItemSO> CraftMultipleWeapons(Rarity rarity, int count)
    {
        List<CraftableItemSO> craftedWeapons = new List<CraftableItemSO>();

        for (int i = 0; i < count; i++)
        {
            CraftableItemSO weapon = CraftRandomWeapon(rarity);
            if (weapon != null)
            {
                craftedWeapons.Add(weapon);
            }
            else
            {
                Debug.Log($"Stopped crafting at {i}/{count} - not enough fragments");
                break;
            }
        }

        return craftedWeapons;
    }

    /// <summary>
    /// Get all weapons of a rarity that player hasn't crafted yet
    /// </summary>
    private List<CraftableItemSO> GetUncraftedWeaponsOfRarity(Rarity rarity)
    {
        List<CraftableItemSO> uncrafted = new List<CraftableItemSO>();

        foreach (var item in craftableItems)
        {
            if (item.rarity == rarity &&
                item.canSocketToSoulWeapon &&
                !craftedItemsData.ContainsKey(item.itemId))
            {
                uncrafted.Add(item);
            }
        }

        return uncrafted;
    }

    /// <summary>
    /// Get all weapons of a rarity (including crafted ones)
    /// </summary>
    private List<CraftableItemSO> GetAllWeaponsOfRarity(Rarity rarity)
    {
        List<CraftableItemSO> allWeapons = new List<CraftableItemSO>();

        foreach (var item in craftableItems)
        {
            if (item.rarity == rarity && item.canSocketToSoulWeapon)
            {
                allWeapons.Add(item);
            }
        }

        return allWeapons;
    }

    /// <summary>
    /// Get fragment cost for a rarity tier
    /// </summary>
    private int GetFragmentCostForRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return 50;
            case Rarity.Rare: return 75;
            case Rarity.Epic: return 100;
            case Rarity.Legendary: return 150;
            case Rarity.Mythical: return 200;
            default: return 50;
        }
    }

    /// <summary>
    /// Get fragment resource ID for a rarity
    /// </summary>
    private string GetFragmentIdForRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return "CommonFragment";
            case Rarity.Rare: return "RareFragment";
            case Rarity.Epic: return "EpicFragment";
            case Rarity.Legendary: return "LegendaryFragment";
            case Rarity.Mythical: return "MythicalFragment";
            default: return "CommonFragment";
        }
    }

    /// <summary>
    /// How much Soul Essence to give for duplicate craft
    /// </summary>
    private int GetDuplicateConversionAmount(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return 5000;
            case Rarity.Rare: return 15000;
            case Rarity.Epic: return 50000;
            case Rarity.Legendary: return 200000;
            case Rarity.Mythical: return 1000000;
            default: return 5000;
        }
    }

    /// <summary>
    /// Check how many weapons player has collected for a rarity
    /// </summary>
    public int GetCollectedWeaponCount(Rarity rarity)
    {
        int count = 0;

        foreach (var item in craftableItems)
        {
            if (item.rarity == rarity &&
                item.canSocketToSoulWeapon &&
                craftedItemsData.ContainsKey(item.itemId))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Get total weapon count for a rarity
    /// </summary>
    public int GetTotalWeaponCount(Rarity rarity)
    {
        int count = 0;

        foreach (var item in craftableItems)
        {
            if (item.rarity == rarity && item.canSocketToSoulWeapon)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Get collection progress text (e.g., "3/5 Common weapons")
    /// </summary>
    public string GetCollectionProgressText(Rarity rarity)
    {
        int collected = GetCollectedWeaponCount(rarity);
        int total = GetTotalWeaponCount(rarity);

        return $"{collected}/{total} {rarity} weapons";
    }

    // Save data using PlayerPrefs
    private void SaveResourceData()
    {
        // Save resources (ensure we don't save negative values)
        foreach (var resource in resourceInventory)
        {
            PlayerPrefs.SetInt("Resource_" + resource.Key, Mathf.Max(0, resource.Value));
        }

        // Save crafted items with level data (ensure we don't save negative values)
        foreach (var item in craftedItemsData)
        {
            PlayerPrefs.SetInt("CraftedItem_" + item.Key, Mathf.Max(0, item.Value.count));
            PlayerPrefs.SetInt("ItemLevel_" + item.Key, Mathf.Max(1, item.Value.level));
        }

        PlayerPrefs.Save();
    }

    // Reset all inventory data
    public void ResetAllData(bool resetUnlocks = false)
    {
        // Reset resource quantities
        foreach (var resource in resourceInventory.Keys.ToList())
        {
            resourceInventory[resource] = 0;
            PlayerPrefs.DeleteKey("Resource_" + resource);
        }

        // Reset crafted item quantities and levels
        foreach (var item in craftedItemsData.Keys.ToList())
        {
            // Reset item data
            craftedItemsData[item].count = 0;
            craftedItemsData[item].level = 1;

            // Reset the copies needed for next level based on level 1
            CraftableItemSO itemData = GetCraftableItemData(item);
            if (itemData != null)
            {
                float levelProgress = 1f / itemData.maxLevel;
                craftedItemsData[item].copiesForNextLevel = Mathf.Max(0, Mathf.RoundToInt(itemData.levelingCurve.Evaluate(levelProgress)));
            }

            // Delete saved data
            PlayerPrefs.DeleteKey("CraftedItem_" + item);
            PlayerPrefs.DeleteKey("ItemLevel_" + item);
        }

        // Optionally reset unlocked resources and items
        if (resetUnlocks)
        {
            // Clear PlayerPrefs keys
            PlayerPrefs.DeleteKey(AVAILABLE_RESOURCES_KEY);
            PlayerPrefs.DeleteKey(AVAILABLE_ITEMS_KEY);

            // Clear lists (they will be repopulated with default values on next start)
            availableResources.Clear();
            craftableItems.Clear();

            // Clear dictionaries
            resourceInventory.Clear();
            craftedItemsData.Clear();

            // Notify UI
            OnAvailableItemsChanged?.Invoke();
        }

        PlayerPrefs.Save();
        OnInventoryChanged?.Invoke();

        Debug.Log("Reset all crafting system data" + (resetUnlocks ? " including unlocks" : ""));
    }

    // Get a dictionary of missing resources for a craftable item
    public Dictionary<ResourceSO, int> GetMissingResources(string itemId)
    {
        Dictionary<ResourceSO, int> missing = new Dictionary<ResourceSO, int>();
        CraftableItemSO item = craftableItems.FirstOrDefault(i => i.itemId == itemId);

        if (item == null)
            return missing;

        foreach (var requirement in item.requirements)
        {
            string reqResourceId = requirement.resource.resourceId;
            int currentAmount = GetResourceAmount(reqResourceId);

            if (currentAmount < requirement.amount)
            {
                int missingAmount = requirement.amount - currentAmount;
                missing.Add(requirement.resource, Mathf.Max(0, missingAmount)); // Ensure non-negative
            }
        }

        return missing;
    }

    // Check if an item can be leveled up
    public bool CanLevelUp(string itemId)
    {
        if (!craftedItemsData.ContainsKey(itemId))
            return false;

        ItemData data = craftedItemsData[itemId];
        CraftableItemSO itemSO = GetCraftableItemData(itemId);
        if (itemSO == null || data.level >= itemSO.maxLevel)
            return false;

        int cost = CalculateWeaponLevelUpCost(data.level);
        return GetResourceAmount("SoulEssence") >= cost;
    }

    // Level up an item
    public bool LevelUpItem(string itemId)
    {
        if (!CanLevelUp(itemId))
            return false;

        ItemData data = craftedItemsData[itemId];
        CraftableItemSO itemSO = GetCraftableItemData(itemId);

        int cost = CalculateWeaponLevelUpCost(data.level);
        RemoveResource("SoulEssence", cost);
        data.level++;

        float levelProgress = (float)data.level / itemSO.maxLevel;
        data.copiesForNextLevel = Mathf.Max(0, Mathf.RoundToInt(itemSO.levelingCurve.Evaluate(levelProgress)));

        SaveResourceData();
        OnInventoryChanged?.Invoke();

        return true;
    }

    // Get the current level of an item
    public int GetItemLevel(string itemId)
    {
        if (craftedItemsData.ContainsKey(itemId))
            return Mathf.Max(1, craftedItemsData[itemId].level); // Ensure at least level 1
        return 0;
    }
    private int CalculateWeaponLevelUpCost(int currentLevel)
    {
        return 1000 * (int)Mathf.Pow(2f, currentLevel - 1);
    }

    // Check if a resource is unlocked
    public bool IsResourceUnlocked(string resourceId)
    {
        return availableResources.Any(r => r.resourceId == resourceId);
    }

    // Check if a craftable item is unlocked
    public bool IsItemUnlocked(string itemId)
    {
        return craftableItems.Any(i => i.itemId == itemId) || craftedItemsData.ContainsKey(itemId);
    }

    // Check and unlock any items that meet their requirements
    public void CheckForUnlockableItems()
    {
        foreach (var item in allItems)
        {
            // Skip already unlocked items
            if (craftableItems.Any(i => i.itemId == item.itemId))
                continue;

            // Check if this item can be unlocked
            if (item.CanBeUnlocked())
            {
                UnlockCraftableItem(item);
                Debug.Log($"Automatically unlocked item {item.displayName} as requirements were met!");
            }
        }
    }

    // Get list of items that can be unlocked but aren't yet
    public List<CraftableItemSO> GetUnlockableItems()
    {
        List<CraftableItemSO> unlockableItems = new List<CraftableItemSO>();

        foreach (var item in allItems)
        {
            // Skip already unlocked items
            if (craftableItems.Any(i => i.itemId == item.itemId))
                continue;

            // Check if this item can be unlocked
            if (item.CanBeUnlocked())
            {
                unlockableItems.Add(item);
            }
        }

        return unlockableItems;
    }

    // Get list of upcoming items with their requirements
    public Dictionary<CraftableItemSO, List<string>> GetUpcomingItems(int maxCount = 5)
    {
        Dictionary<CraftableItemSO, List<string>> upcomingItems = new Dictionary<CraftableItemSO, List<string>>();
        int count = 0;

        foreach (var item in allItems)
        {
            // Skip already unlocked items
            if (craftableItems.Any(i => i.itemId == item.itemId))
                continue;

            // Get unmet requirements
            List<string> unmetRequirements = item.GetUnmetRequirements();

            // Only show items that have some requirements met (not all requirements unmet)
            if (unmetRequirements.Count > 0 && unmetRequirements.Count < item.unlockRequirements.Count)
            {
                upcomingItems.Add(item, unmetRequirements);
                count++;

                if (count >= maxCount)
                    break;
            }
        }

        return upcomingItems;
    }

    // Call this method whenever game state changes that might affect unlocks
    public void CheckUnlockProgress()
    {
        // Check for newly unlockable items that aren't already unlocked
        foreach (var item in allItems)
        {
            if (!craftableItems.Any(i => i.itemId == item.itemId))
            {
                if (item.CanBeUnlocked())
                {
                    UnlockCraftableItem(item);
                }
            }
        }

        // Check for newly unlockable resources that aren't already unlocked
        foreach (var resource in allResources)
        {
            if (!availableResources.Any(r => r.resourceId == resource.resourceId))
            {
                // Add logic here if resources have unlock requirements
                // For now, resources are unlocked manually via UnlockResource
            }
        }
    }
}

[System.Serializable]
public class ItemData
{
    public int count;
    public int level;
    public int copiesForNextLevel;
}