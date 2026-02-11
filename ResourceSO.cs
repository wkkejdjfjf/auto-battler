using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Resource", menuName = "Crafting/Resource")]
public class ResourceSO : ScriptableObject
{
    public string resourceId;
    public string displayName;
    public Sprite icon;
    public string description;

    // Rarity/tier
    public int tier = 1;

    // Unlock requirements (resources could also have unlock requirements)
    public List<UnlockRequirement> unlockRequirements = new List<UnlockRequirement>();

    // Optional custom attributes
    public Color resourceColor = Color.white;
    public bool isRawMaterial = true;

    // Check if all unlock requirements are met
    public bool CanBeUnlocked()
    {
        if (unlockRequirements == null || unlockRequirements.Count == 0)
            return true;

        foreach (var requirement in unlockRequirements)
        {
            if (!requirement.IsMet())
                return false;
        }

        return true;
    }

    // Get list of unmet requirements for UI display
    public List<string> GetUnmetRequirements()
    {
        List<string> unmetRequirements = new List<string>();

        if (unlockRequirements == null || unlockRequirements.Count == 0)
            return unmetRequirements;

        foreach (var requirement in unlockRequirements)
        {
            if (!requirement.IsMet())
            {
                unmetRequirements.Add(requirement.GetDescription());
            }
        }

        return unmetRequirements;
    }
}