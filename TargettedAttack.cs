using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TargettedAttack : MonoBehaviour
{
    public double power;
    private double damage;
    public float buff;
    public float range;
    public float delay;
    public GameObject shooter;
    [HideInInspector]
    public double characterAtk;
    [HideInInspector]
    public float characterCritMulti;
    [HideInInspector]
    public float characterCritPerc;

    private DynamicTextData data;
    public DynamicTextData normData;
    public DynamicTextData critData;

    private List<EnemyStats> enemies;

    public GameObject attackObject;

    private void Start()
    {
        damage = 0;
        if (Random.value < (characterCritPerc / 100))
        {
            data = critData;
            damage = (power / 100) * characterCritMulti * (1 + (buff / 100)) * characterAtk;
        }
        else
        {
            data = normData;
            damage = (power / 100) * (1 + (buff / 100)) * characterAtk;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(shooter.transform.position, range);
        List<EnemyStats> enemies = new List<EnemyStats>();

        foreach (Collider2D hitcollider in hitColliders)
        {
            EnemyStats enemy = hitcollider.gameObject.GetComponent<EnemyStats>();
            enemies.Add(enemy);
            if (enemy != null)
            {
                Instantiate(attackObject, hitcollider.transform.position, Quaternion.identity);
                
            }
        }

        StartCoroutine(Damage(enemies));

    }

    IEnumerator Damage(List<EnemyStats> enemy)
    {
        yield return new WaitForSeconds(delay);
        foreach (EnemyStats _enemy in enemy)   
        {
            if (_enemy != null)
            {
                _enemy.TakeDamage(damage, data);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
