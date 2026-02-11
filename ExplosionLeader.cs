using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionLeader : MonoBehaviour
{
    public Projectile projectile;

    public MultiSlash leadingEffect;

    private void Start()
    {
        MultiSlash slash = Instantiate(leadingEffect, transform.position, projectile.transform.rotation);
        slash.shooter = projectile.shooter;
        slash.buff = projectile.buff;
        slash.characterAtk = projectile.characterAtk;
        slash.characterCritMulti = projectile.characterCritMulti;
        slash.characterCritPerc = projectile.characterCritPerc;    
    }
}
