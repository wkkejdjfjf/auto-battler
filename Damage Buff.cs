using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageBuff : MonoBehaviour
{
    public float damageBuff;
    public float duration;
    public GameObject buffEffect;
    public Abilities abilitiesScript;

    private void Start()
    {
        abilitiesScript = gameObject.GetComponent<Abilities>();
        StartCoroutine(StartBuff());
    }

    IEnumerator StartBuff()
    {
        abilitiesScript.damageBuff += damageBuff;
        var Buff = Instantiate(buffEffect, gameObject.transform);
        yield return new WaitForSeconds(duration);
        abilitiesScript.damageBuff -= damageBuff;
        Destroy(Buff);
        Destroy(this);
    }
}
