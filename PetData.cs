using UnityEngine;

[CreateAssetMenu(fileName = "New Pet Data", menuName = "Pets/Pet Data")]
public class PetData : ScriptableObject
{
    [Header("Basic Info")]
    public string petName;
    public Rarity rarity;
    public Sprite petSprite;

    [Header("Abilities")]
    public Ability specialAbility;

    [Header("Visuals")]
    public GameObject petPrefab; // Link to the visual prefab if needed
}