using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text goldText;
    [SerializeField]
    private TMP_Text atkText;
    [SerializeField]
    private TMP_Text defText;
    [SerializeField]
    private TMP_Text healthText;
    private PlayerStats charStats;

    private void Start()
    {
        charStats = FindFirstObjectByType<PlayerStats>();
        StartCoroutine(UpdateUI());
    }

    IEnumerator UpdateUI()
    {
        UpdateGoldUI();
        UpdateHealthUI();
        UpdateAtkUI();
        UpdateDefUI();
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(UpdateUI());
    }

    public void UpdateGoldUI()
    {
        goldText.text = BigNumberFormatter.Format(charStats.gold);
    }

    public void UpdateAtkUI()
    {
        atkText.text = BigNumberFormatter.Format(charStats.attack);
    }

    public void UpdateDefUI()
    {
        defText.text = BigNumberFormatter.Format(charStats.defense);
    }

    public void UpdateHealthUI()
    {
        healthText.text = BigNumberFormatter.Format(charStats.maxHealth, 0);
    }
}