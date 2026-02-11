using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;

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
            Debug.Log("No saved resources found, using initial resources from inspector");
        }
    }

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
            Debug.Log("No saved items found, using initial items from inspector");
        }
    }

    private void SaveAvailableResources()
    {
        string resourceIdsString = string.Join(",", availableResources.Select(r => r.resourceId));
        PlayerPrefs.SetString(AVAILABLE_RESOURCES_KEY, resourceIdsString);
        PlayerPrefs.Save();
    }

    private void SaveAvailableItems()
    {
        string itemIdsString = string.Join(",", craftableItems.Select(i => i.itemId));
        PlayerPrefs.SetString(AVAILABLE_ITEMS_KEY, itemIdsString);
        PlayerPrefs.Save();
    }

    public void InitializeInventory()
    {
        resourceInventory.Clear();
        craftedItemsData.Clear();

        foreach (var resource in availableResources)
        {
            int savedAmount = PlayerPrefs.GetInt("Resource_" + resource.resourceId, 0);
            resourceInventory[resource.resourceId] = Mathf.Max(0, savedAmount);
        }

        foreach (var item in craftableItems)
        {
            ItemData data = new ItemData();
            data.count = Mathf.Max(0, PlayerPrefs.GetInt("CraftedItem_" + item.itemId, 0));
            data.level = Mathf.Max(1, PlayerPrefs.GetInt("ItemLevel_" + item.itemId, 1));

            craftedItemsData[item.itemId] = data;
        }

        OnInventoryChanged?.Invoke();
    }

    public void UnlockResource(ResourceSO resource)
    {
        if (!availableResources.Any(r => r.resourceId == resource.resourceId))
        {
            availableResources.Add(resource);

            if (!resourceInventory.ContainsKey(resource.resourceId))
            {
                resourceInventory.Add(resource.resourceId, 0);
            }

            SaveAvailableResources();

            OnAvailableItemsChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            OnResourceUnlocked?.Invoke(resource.resourceId);

            Debug.Log($"Unlocked new resource: {resource.resourceId}");
        }
    }

    public void UnlockCraftableItem(CraftableItemSO item)
    {
        if (!craftableItems.Any(i => i.itemId == item.itemId))
        {
            craftableItems.Add(item);

            if (!craftedItemsData.ContainsKey(item.itemId))
            {
                ItemData data = new ItemData();
                data.count = 0;
                data.level = 1;

                craftedItemsData[item.itemId] = data;
            }

            SaveAvailableItems();

            OnAvailableItemsChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            OnItemUnlocked?.Invoke(item.itemId);

            Debug.Log($"Unlocked new craftable item: {item.itemId}");
        }
    }

    public void AddResource(string resourceId, int amount)
    {
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

            CheckForUnlockableItems();
        }
        else
        {
            ResourceSO resource = allResources.FirstOrDefault(r => r.resourceId == resourceId);
            if (resource != null)
            {
                UnlockResource(resource);
                resourceInventory[resourceId] = resourceInventory[resourceId] + amount;
                SaveResourceData();
                OnInventoryChanged?.Invoke();

                CheckForUnlockableItems();
            }
            else
            {
                Debug.LogWarning($"Failed to add resource: {resourceId} not found in allResources list");
            }
        }
    }

    public void RemoveResource(string resourceId, int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"RemoveResource called with non-positive amount ({amount}) for {resourceId}.");
            return;
        }

        if (resourceInventory.ContainsKey(resourceId))
        {
            resourceInventory[resourceId] = Mathf.Max(0, resourceInventory[resourceId] - amount);
            SaveResourceData();
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"Cannot remove resource {resourceId}: not in inventory");
        }
    }

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

    public int GetResourceAmount(string resourceId)
    {
        if (resourceInventory.ContainsKey(resourceId))
        {
            return Mathf.Max(0, resourceInventory[resourceId]);
        }
        return 0;
    }

    public ResourceSO GetResourceData(string resourceId)
    {
        return availableResources.FirstOrDefault(r => r.resourceId == resourceId);
    }

    public int GetCraftedItemAmount(string itemId)
    {
        if (craftedItemsData.ContainsKey(itemId))
        {
            return Mathf.Max(0, craftedItemsData[itemId].count);
        }
        return 0;
    }

    public CraftableItemSO GetCraftableItemData(string itemId)
    {
        return craftableItems.FirstOrDefault(i => i.itemId == itemId);
    }

    public bool CanCraft(string itemId, int amount = 1)
    {
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

    public bool CraftItem(string itemId, int amount = 1)
    {
        if (amount <= 0)
            return false;

        if (!CanCraft(itemId, amount))
            return false;

        CraftableItemSO item = craftableItems.FirstOrDefault(i => i.itemId == itemId);

        foreach (var requirement in item.requirements)
        {
            string reqResourceId = requirement.resource.resourceId;
            int requiredAmount = requirement.amount * amount;

            resourceInventory[reqResourceId] = Mathf.Max(0, resourceInventory[reqResourceId] - requiredAmount);
        }

        if (!craftedItemsData.ContainsKey(itemId))
        {
            ItemData data = new ItemData();
            data.level = 1;
            data.count = 0;

            craftedItemsData[itemId] = data;
        }

        craftedItemsData[itemId].count = Mathf.Max(0, craftedItemsData[itemId].count + amount);

        SaveResourceData();
        OnInventoryChanged?.Invoke();

        CheckForUnlockableItems();

        return true;
    }

    private void SaveResourceData()
    {
        foreach (var resource in resourceInventory)
        {
            PlayerPrefs.SetInt("Resource_" + resource.Key, Mathf.Max(0, resource.Value));
        }

        foreach (var item in craftedItemsData)
        {
            PlayerPrefs.SetInt("CraftedItem_" + item.Key, Mathf.Max(0, item.Value.count));
            PlayerPrefs.SetInt("ItemLevel_" + item.Key, Mathf.Max(1, item.Value.level));
        }

        PlayerPrefs.Save();
    }

    public void ResetAllData(bool resetUnlocks = false)
    {
        foreach (var resource in resourceInventory.Keys.ToList())
        {
            resourceInventory[resource] = 0;
            PlayerPrefs.DeleteKey("Resource_" + resource);
        }

        foreach (var item in craftedItemsData.Keys.ToList())
        {
            craftedItemsData[item].count = 0;
            craftedItemsData[item].level = 1;

            PlayerPrefs.DeleteKey("CraftedItem_" + item);
            PlayerPrefs.DeleteKey("ItemLevel_" + item);
        }

        if (resetUnlocks)
        {
            PlayerPrefs.DeleteKey(AVAILABLE_RESOURCES_KEY);
            PlayerPrefs.DeleteKey(AVAILABLE_ITEMS_KEY);

            availableResources.Clear();
            craftableItems.Clear();

            resourceInventory.Clear();
            craftedItemsData.Clear();

            OnAvailableItemsChanged?.Invoke();
        }

        PlayerPrefs.Save();
        OnInventoryChanged?.Invoke();

        Debug.Log("Reset all crafting system data" + (resetUnlocks ? " including unlocks" : ""));
    }

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
                missing.Add(requirement.resource, Mathf.Max(0, missingAmount));
            }
        }

        return missing;
    }

    public bool CanLevelUp(string itemId)
    {
        if (!craftedItemsData.ContainsKey(itemId))
            return false;

        ItemData data = craftedItemsData[itemId];
        CraftableItemSO item = GetCraftableItemData(itemId);

        if (item == null)
            return false;

        if (data.level >= item.maxLevel)
            return false;

        // Check if player has enough Soul Essence
        int soulEssenceCost = CalculateWeaponLevelUpCost(data.level);
        return GetResourceAmount("SoulEssence") >= soulEssenceCost;
    }

    public  int CalculateWeaponLevelUpCost(int currentLevel)
    {
        int baseCost = 1000;
        return baseCost * (int)Mathf.Pow(2f, currentLevel - 1);
    }

    public bool LevelUpItem(string itemId)
    {
        if (!CanLevelUp(itemId))
            return false;

        ItemData data = craftedItemsData[itemId];

        // Consume Soul Essence
        int soulEssenceCost = CalculateWeaponLevelUpCost(data.level);
        RemoveResource("SoulEssence", soulEssenceCost);

        data.level++;

        SaveResourceData();
        OnInventoryChanged?.Invoke();
        CheckForUnlockableItems();

        return true;
    }

    public int GetItemLevel(string itemId)
    {
        if (craftedItemsData.ContainsKey(itemId))
            return Mathf.Max(1, craftedItemsData[itemId].level);
        return 0;
    }

    public bool IsResourceUnlocked(string resourceId)
    {
        return availableResources.Any(r => r.resourceId == resourceId);
    }

    public bool IsItemUnlocked(string itemId)
    {
        return craftableItems.Any(i => i.itemId == itemId);
    }

    public void CheckForUnlockableItems()
    {
        foreach (var item in allItems)
        {
            if (craftableItems.Any(i => i.itemId == item.itemId))
                continue;

            if (item.CanBeUnlocked())
            {
                UnlockCraftableItem(item);
                Debug.Log($"Automatically unlocked item {item.displayName} as requirements were met!");
            }
        }
    }

    public List<CraftableItemSO> GetUnlockableItems()
    {
        List<CraftableItemSO> unlockableItems = new List<CraftableItemSO>();

        foreach (var item in allItems)
        {
            if (craftableItems.Any(i => i.itemId == item.itemId))
                continue;

            if (item.CanBeUnlocked())
            {
                unlockableItems.Add(item);
            }
        }

        return unlockableItems;
    }

    public Dictionary<CraftableItemSO, List<string>> GetUpcomingItems(int maxCount = 5)
    {
        Dictionary<CraftableItemSO, List<string>> upcomingItems = new Dictionary<CraftableItemSO, List<string>>();
        int count = 0;

        foreach (var item in allItems)
        {
            if (craftableItems.Any(i => i.itemId == item.itemId))
                continue;

            List<string> unmetRequirements = item.GetUnmetRequirements();

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

    public void CheckUnlockProgress()
    {
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

        foreach (var resource in allResources)
        {
            if (!availableResources.Any(r => r.resourceId == resource.resourceId))
            {
                // Add logic here if resources have unlock requirements
            }
        }
    }

    // ============================================
    // RARITY-BASED WEAPON CRAFTING METHODS
    // ============================================

    public CraftableItemSO CraftRandomWeapon(Rarity rarity)
    {
        int fragmentCost = GetFragmentCostForRarity(rarity);
        string fragmentId = GetFragmentIdForRarity(rarity);

        if (GetResourceAmount(fragmentId) < fragmentCost)
        {
            Debug.Log($"Not enough {fragmentId}. Need {fragmentCost}, have {GetResourceAmount(fragmentId)}");
            return null;
        }

        List<CraftableItemSO> weaponPool = GetUncraftedWeaponsOfRarity(rarity);

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

        CraftableItemSO randomWeapon = weaponPool[UnityEngine.Random.Range(0, weaponPool.Count)];

        RemoveResource(fragmentId, fragmentCost);

        bool isDuplicate = craftedItemsData.ContainsKey(randomWeapon.itemId);

        if (isDuplicate)
        {
            int essenceReward = GetDuplicateConversionAmount(rarity);
            AddResource("SoulEssence", essenceReward);

            Debug.Log($"Duplicate {randomWeapon.displayName}! Converted to {essenceReward} Soul Essence");
        }
        else
        {
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

    public string GetCollectionProgressText(Rarity rarity)
    {
        int collected = GetCollectedWeaponCount(rarity);
        int total = GetTotalWeaponCount(rarity);

        return $"{collected}/{total} {rarity} weapons";
    }

    public IReadOnlyDictionary<string, ItemData> GetCraftedItems()
    {
        return craftedItemsData;
    }

    public bool IsDuplicate(string itemId)
    {
        return craftedItemsData.ContainsKey(itemId)
            && craftedItemsData[itemId].count > 1;
    }
    public int GetSoulEssenceAmount()
    {
        return resourceInventory["SoulEssence"];
    }


}

[System.Serializable]
public class ItemData
{
    public int count;
    public int level;
}
