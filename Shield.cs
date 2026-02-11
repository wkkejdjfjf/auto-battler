using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[CreateAssetMenu]
public class Shield : Ability
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float maxhits = 3f;

    [SerializeField] private GameObject shieldPrefab;

    public override void Activate(GameObject parent)
    {
        
        var shield = Instantiate(shieldPrefab, parent.transform.position, parent.transform.rotation);
        shield.GetComponent<ShieldEffect>().shooter = parent;
        shield.GetComponent<ShieldEffect>().duration = duration;
        shield.GetComponent<ShieldEffect>().maxHits = maxhits;
    }
}
