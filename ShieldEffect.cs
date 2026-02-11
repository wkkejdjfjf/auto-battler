using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShieldEffect : MonoBehaviour
{
    public float duration;
    public float maxHits;
    private float timesHit;
    [SerializeField] private float delay;
    public GameObject shieldHitPrefab;
    public GameObject shooter;
    private void Start()
    {
        Destroy(gameObject, duration);
        tag = shooter.tag;
        StartCoroutine(StartRoutine());
    }

    IEnumerator StartRoutine()
    {
        yield return new WaitForSeconds(delay);

    }

    private HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log($"{gameObject.name} collided with {collision.name} (tag: {collision.tag})");

        if (alreadyHit.Contains(collision.gameObject)) return;

        if (collision.tag == "Shield")
        {
            //Debug.Log("Shield collision detected - returning early");
            return;
        }

        if (collision.tag != shooter.tag && timesHit < maxHits)
        {      
            alreadyHit.Add(collision.gameObject);
            //print($"Hit by: {collision.name} (tag: {collision.tag}) - timesHit: {timesHit}, maxHits: {maxHits}");
            timesHit++;
            Instantiate(shieldHitPrefab, collision.transform.position, Quaternion.identity);
            if (timesHit >= maxHits)
            {
                Destroy(gameObject);
            }
            Destroy(collision.gameObject);

        }
    }

}
