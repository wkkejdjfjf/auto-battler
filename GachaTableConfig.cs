using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

[System.Serializable]
public class GachaTableConfig
{
    [Header("Table Settings")]
    public string tableName;
    public List<GachaItem> items;

    [Header("Pity System")]
    public bool enablePitySystem = true;
    public int pityThreshold = 10;
    public Rarity guaranteedRarity = Rarity.Epic;

    [Header("Level-based Rarity Rates")]
    public List<GachaLevelConfig> levelConfigs = new List<GachaLevelConfig>();

    [Header("Debug Info")]
    [SerializeField] private float totalWeight;
    [SerializeField] private bool isValid;

    private int currentPityCount = 0;

    public void AddItem(GachaItem item, float probability)
    {
        items.Add(item);
        ValidateTable();
    }

    public void RemoveItem(GachaItem item)
    {
        items.Remove(item);
        ValidateTable();
    }

    public void SetItemProbability(GachaItem item, float newProbability)
    {
        if (items.Contains(item))
        {
            item.probability = newProbability;
            Debug.Log($"Set probability of {item.name} to {newProbability}");
        }
        else
        {
            Debug.LogWarning($"Item {item.name} not found in table {tableName}");
        }
    }

    public bool ValidateTable()
    {
        totalWeight = items.Sum(item => item.probability);
        isValid = totalWeight > 0f && items.Count > 0;

        if (!isValid)
            Debug.LogError($"Table '{tableName}' is invalid! Weight: {totalWeight}, Count: {items.Count}");

        return isValid;
    }

    public GachaItem Roll()
    {
        if (!isValid && !ValidateTable())
        {
            return null;
        }

        bool shouldTriggerPity = false;
        if (enablePitySystem)
        {
            currentPityCount++;
            if (currentPityCount >= pityThreshold)
            {
                currentPityCount = 0;
                shouldTriggerPity = true;
            }
        }

        GachaItem result;
        if (shouldTriggerPity)
        {
            result = RollGuaranteed();
        }
        else
        {
            result = RollNormal();

            if (enablePitySystem && result != null && result.rarity >= guaranteedRarity)
            {
                currentPityCount = 0;
            }
        }
        return result;
    }
    public List<GachaItem> RollMultiple(int count)
    {
        List<GachaItem> results = new List<GachaItem>();
        for (int i = 0; i < count; i++)
        {
            results.Add(Roll());
        }
        return results;
    }

    private GachaItem RollNormal()
    {
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var item in items)
        {
            currentWeight += item.probability;
            if (randomValue <= currentWeight)
            {
                return item;
            }
        }

        return items[items.Count - 1];
    }

    private GachaItem RollGuaranteed()
    {
        List<GachaItem> validItems = new List<GachaItem>();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].rarity >= guaranteedRarity)
            {
                validItems.Add(items[i]);
            }
        }

        if (validItems.Count == 0)
        {
            return RollNormal();
        }

        float validWeight = validItems.Sum(item => item.probability);
        float randomValue = Random.Range(0f, validWeight);
        float currentWeight = 0f;

        foreach (var item in validItems)
        {
            currentWeight += item.probability;
            if (currentWeight >= randomValue)
            {
                return item;
            }
        }

        return items[items.Count - 1];
    }

    public Dictionary<GachaItem, float> GetNormalizedPercentages()
    {
        if (!isValid && !ValidateTable())
        {
            return new Dictionary<GachaItem, float>();
        }

        var percentages = new Dictionary<GachaItem, float>();

        foreach (GachaItem item in items)
        {
            float normalizedPercentage = Mathf.Round((item.probability / totalWeight) * 100 * 10) / 10;
            percentages[item] = normalizedPercentage;
        }

        return percentages;
    }
    public Dictionary<Rarity, float> GetRarityRates()
    {
        var rates = new Dictionary<Rarity, float>();
        var percentages = GetNormalizedPercentages();

        foreach (var kvp in percentages)
        {
            if (!rates.ContainsKey(kvp.Key.rarity))
                rates[kvp.Key.rarity] = 0f;
            rates[kvp.Key.rarity] += kvp.Value;
        }

        return rates;
    }
    public int GetPityCount() => currentPityCount;

    public void ResetPityCount()
    {
        currentPityCount = 0;
    }

    public void ShowProbabilities()
    {
        Debug.Log($"=== {tableName} Probabilities ===");
        var percentages = GetNormalizedPercentages();

        foreach (var kvp in percentages)
        {
            Debug.Log($"{kvp.Key.name}: {kvp.Value:F2}% ({kvp.Key.rarity})");
        }

        if (enablePitySystem)
        {
            Debug.Log($"Pity: {currentPityCount}/{pityThreshold} (guarantees {guaranteedRarity}+)");
        }

        var rarityRates = GetRarityRates();
        Debug.Log("By Rarity:");
        foreach (var kvp in rarityRates)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value:F2}%");
        }
    }
}
