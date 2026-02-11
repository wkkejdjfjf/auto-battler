using System;
using System.Collections.Generic;
using UnityEngine;

// Base class for all unlock requirements
[Serializable]
public abstract class UnlockRequirement
{
    public abstract bool IsMet();
    public abstract string GetDescription();
}

// Resource quantity requirement
[Serializable]
public class ResourceQuantityRequirement : UnlockRequirement
{
    public ResourceSO resource;
    public int requiredAmount;

    public override bool IsMet()
    {
        if (CraftingSystem.Instance == null) return false;
        return CraftingSystem.Instance.GetResourceAmount(resource.resourceId) >= requiredAmount;
    }

    public override string GetDescription()
    {
        return $"Collect {requiredAmount} {resource.displayName}";
    }
}

// Item crafting requirement
[Serializable]
public class ItemCraftedRequirement : UnlockRequirement
{
    public CraftableItemSO item;
    public int requiredAmount;

    public override bool IsMet()
    {
        if (CraftingSystem.Instance == null) return false;
        return CraftingSystem.Instance.GetCraftedItemAmount(item.itemId) >= requiredAmount;
    }

    public override string GetDescription()
    {
        return $"Craft {requiredAmount} {item.displayName}";
    }
}

// Player level requirement
[Serializable]
public class PlayerLevelRequirement : UnlockRequirement
{
    public int requiredLevel;

    public override bool IsMet()
    {
        // Replace this with your actual player level check
        // Example implementation assuming you have a PlayerManager or similar
        if (LevelSystem.Instance == null) return false;
        return LevelSystem.Instance.GetCurrentLevel() >= requiredLevel;
    }

    public override string GetDescription()
    {
        return $"Reach player level {requiredLevel}";
    }
}

// Item level requirement
[Serializable]
public class ItemLevelRequirement : UnlockRequirement
{
    public CraftableItemSO item;
    public int requiredLevel;

    public override bool IsMet()
    {
        if (CraftingSystem.Instance == null) return false;
        return CraftingSystem.Instance.GetItemLevel(item.itemId) >= requiredLevel;
    }

    public override string GetDescription()
    {
        return $"Upgrade {item.displayName} to level {requiredLevel}";
    }
}

// Resource variety requirement
[Serializable]
public class ResourceVarietyRequirement : UnlockRequirement
{
    public List<ResourceSO> requiredResources;
    public int minimumAmount = 1;

    public override bool IsMet()
    {
        if (CraftingSystem.Instance == null) return false;

        foreach (var resource in requiredResources)
        {
            if (CraftingSystem.Instance.GetResourceAmount(resource.resourceId) < minimumAmount)
            {
                return false;
            }
        }

        return true;
    }

    public override string GetDescription()
    {
        string resourceList = string.Join(", ", requiredResources.ConvertAll(r => r.displayName));
        return $"Collect at least {minimumAmount} of each: {resourceList}";
    }
}

// Resource unlock requirement
[Serializable]
public class ResourceUnlockedRequirement : UnlockRequirement
{
    public List<ResourceSO> requiredResources;

    public override bool IsMet()
    {
        if (CraftingSystem.Instance == null) return false;

        foreach (var resource in requiredResources)
        {
            if (!CraftingSystem.Instance.IsResourceUnlocked(resource.resourceId))
            {
                return false;
            }
        }

        return true;
    }

    public override string GetDescription()
    {
        string resourceList = string.Join(", ", requiredResources.ConvertAll(r => r.displayName));
        return $"Unlock resources: {resourceList}";
    }
}

// Item unlock requirement
[Serializable]
public class ItemUnlockedRequirement : UnlockRequirement
{
    public List<CraftableItemSO> requiredItems;

    public override bool IsMet()
    {
        if (CraftingSystem.Instance == null) return false;

        foreach (var item in requiredItems)
        {
            if (!CraftingSystem.Instance.IsItemUnlocked(item.itemId))
            {
                return false;
            }
        }

        return true;
    }

    public override string GetDescription()
    {
        string itemList = string.Join(", ", requiredItems.ConvertAll(i => i.displayName));
        return $"Unlock items: {itemList}";
    }
}