using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class MultiHit : Ability
{
    public MultiSlash abilityObject;

    public override void Activate(GameObject parent, float buff, double atkstat, float critMulti, float critPerc)
    {
        var multiSlash = Instantiate(abilityObject, parent.transform.position, Quaternion.identity);
        multiSlash.GetComponent<MultiSlash>().shooter = parent;
        multiSlash.GetComponent<MultiSlash>().buff = buff;
        multiSlash.GetComponent<MultiSlash>().characterAtk = atkstat;
        multiSlash.GetComponent<MultiSlash>().characterCritMulti = critMulti;
        multiSlash.GetComponent<MultiSlash>().characterCritPerc = critPerc;
    }
}
