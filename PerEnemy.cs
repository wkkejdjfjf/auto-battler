using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-enemy targeted ability that spawns an attack for each enemy in range
/// </summary>
[CreateAssetMenu(fileName = "New Per Enemy Ability", menuName = "Abilities/Per Enemy")]
public class PerEnemy : Ability
{
    public TargettedAttack abilityObject;

    /// <summary>
    /// Activate the ability with full combat stats
    /// </summary>
    public override void Activate(GameObject parent, float buff, double atkstat, float critDamage, float critChance)
    {
        var targettedAttack = Instantiate(abilityObject);
        targettedAttack.GetComponent<TargettedAttack>().shooter = parent;
        targettedAttack.GetComponent<TargettedAttack>().buff = buff;
        targettedAttack.GetComponent<TargettedAttack>().characterAtk = atkstat;
        targettedAttack.GetComponent<TargettedAttack>().characterCritMulti = critDamage;
        targettedAttack.GetComponent<TargettedAttack>().characterCritPerc = critChance;
    }
}