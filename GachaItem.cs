using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[System.Serializable]
public class GachaItem
{
    public string name;
    public Sprite icon;
    public Rarity rarity;
    public float probability;
}
[System.Serializable]
public class RarityProbability
{
    public Rarity rarity;
    [Range(0f, 100f)]
    public float probability; // percentage for this rarity
}

[System.Serializable]
public class GachaLevelConfig
{
    public int level;
    public List<RarityProbability> rarities = new List<RarityProbability>();
}
