using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOEEffect : MonoBehaviour
{
    public float radius = 5f;
    public double buff = 1f;
    public double atkstat = 1f;
    public double critMulti = 1f;
    public double critPerc = 0f;
    public float delay = 0f;


    private DynamicTextData data;
    public DynamicTextData normData;
    public DynamicTextData critData;

    public GameObject effectObj;


    private void Start()
    {
        double damage = 0;
        if (Random.value <= (critPerc / 100))
        {
            data = critData;
            damage = critMulti * (1 + (buff / 100)) * atkstat;
        }
        else
        {
            data = normData;
            damage = (1 + (buff / 100)) * atkstat;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius);
        List<EnemyStats> enemies = new List<EnemyStats>();

        foreach (Collider2D collider in hitColliders)
        {
            EnemyStats enemy = collider.GetComponent<EnemyStats>();
            if (enemy != null)
            {
                enemies.Add(enemy);
            }

        }
        Instantiate(effectObj, new Vector3(enemies[0].transform.position.x, enemies[0].transform.position.y + 8.41f, enemies[0].transform.position.z), effectObj.transform.rotation);
        StartCoroutine(routine(damage));
    }

    IEnumerator routine(double damage)
    {
        yield return new WaitForSeconds(delay);

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius);
        List<EnemyStats> enemies = new List<EnemyStats>();

        foreach (Collider2D collider in hitColliders)
        {
            EnemyStats enemy = collider.GetComponent<EnemyStats>();
            if (enemy != null)
            {
                enemies.Add(enemy);
            }
        }

        foreach (EnemyStats enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(damage, data);
            }
        }
    }
}
