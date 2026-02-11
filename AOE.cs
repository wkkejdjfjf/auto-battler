using UnityEngine;

[CreateAssetMenu]
public class AOE : Ability
{
    public GameObject aoeEffect;

    public override void Activate(GameObject parent)
    {
        ActivateAOE(parent, 1f, 1f, 1f, 0f);
    }

    public override void Activate(GameObject parent, float buff, double atkstat, float critMulti, float critPerc)
    {
        ActivateAOE(parent, buff, atkstat, critMulti, critPerc);
    }

    private void ActivateAOE(GameObject parent, double buff, double atkstat, double critMulti, double critPerc)
    {
        var aoeInstance = Instantiate(aoeEffect, new Vector3(parent.transform.position.x + 3, parent.transform.position.y, parent.transform.position.z), Quaternion.identity);
        AOEEffect effect = aoeInstance.GetComponent<AOEEffect>();
        if (effect != null)
        {
            effect.buff = buff;
            effect.atkstat = atkstat;
            effect.critMulti = critMulti;
            effect.critPerc = critPerc;
        }
    }
}
