using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Linked Weapon")]

public class LinkedWeapon : MonoBehaviour
{
    public string weaponName;
    public Sprite weaponIcon;
    //public WeaponElement element;

    [Header("Stats When Linked")]
    public float attackBonus = 0.15f;
    public float critDamage = 0.08f;

    [Header("Active Skill")]
    public Ability weaponSkill;
    public float skillCooldown;

    [Header("Resonance")]
    public int resonanceLevel = 1;
    public int maxResonance = 10;

    [Header("Crafting")]
    public int fragmentsRequired = 100;
    public WeaponFragment fragmentType;
}
