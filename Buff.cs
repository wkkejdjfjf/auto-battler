using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu]
public class Buff : Ability
{
    public float damageBuff;
    public float duration;
    public GameObject buffEffect;
    public DamageBuff buffScript;

    public override void Activate(GameObject parent)
    {
        buffScript = parent.AddComponent<DamageBuff>();
        buffScript.buffEffect = buffEffect;
        buffScript.duration = duration;
        buffScript.damageBuff = damageBuff;
    }
}
