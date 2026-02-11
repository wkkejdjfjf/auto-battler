using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu]
public class Heal : Ability
{
    public float healAmount;
    public float healPerc;
    public GameObject healEffect;

    public override void Activate(GameObject parent)
    {
        double healamt = healAmount + parent.GetComponent<CharacterStatsBase>().maxHealth * (healPerc / 100);
        parent.GetComponent<CharacterStatsBase>().Heal(healamt);
        Instantiate(healEffect, parent.transform);
    }
}
