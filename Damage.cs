using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[CreateAssetMenu]
public class Damage : Ability
{
    public GameObject projectile;

    public override void Activate(GameObject parent)
    {
        var Projectile = Instantiate(projectile, parent.GetComponent<Abilities>().shootPoint.position, parent.GetComponent<Abilities>().shootPoint.rotation);
        Projectile.GetComponent<Projectile>().shooter = parent;
    }

    public override void Activate(GameObject parent, float buff, double atkstat, float critMulti, float critPerc)
    {
        var Projectile = Instantiate(projectile, parent.GetComponent<Abilities>().shootPoint.position, parent.GetComponent<Abilities>().shootPoint.rotation);
        Projectile.GetComponent<Projectile>().shooter = parent;
        Projectile.GetComponent<Projectile>().buff = buff;
        Projectile.GetComponent<Projectile>().characterAtk = atkstat;
        Projectile.GetComponent<Projectile>().characterCritMulti = critMulti;
        Projectile.GetComponent<Projectile>().characterCritPerc = critPerc;
    }
}
