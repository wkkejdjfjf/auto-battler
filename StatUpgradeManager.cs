using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class StatUpgradeManager : MonoBehaviour
{
    [Header("References")]
    private PlayerStats playerStats;
    public TextMeshProUGUI baseAtkText;
    public TextMeshProUGUI baseDefText;
    public TextMeshProUGUI baseHealthText;

    [Header("Upgrade Settings")]
    public double attackUpgradeAmount = 1f;
    public double defenseUpgradeAmount = 1f;
    public double healthUpgradeAmount = 10f;

    [Header("Cost Scaling Settings")]
    [Tooltip("Base cost for stat upgrades")]
    public float baseCost = 100f;

    [Tooltip("Cost increase per upgrade level")]
    public float costIncreasePerLevel = 25f;

    [Tooltip("Exponential scaling factor for upgrades")]
    public float costExponent = 1.025f;

    [Tooltip("Minimum cost for upgrades")]
    public float minCost = 50f;

    [Header("UI")]
    public Button attackUpgradeButton;
    public Button defenseUpgradeButton;
    public Button healthUpgradeButton;
    public TextMeshProUGUI attackCostText;
    public TextMeshProUGUI defenseCostText;
    public TextMeshProUGUI healthCostText;
    public TextMeshProUGUI upgradedAtk;
    public TextMeshProUGUI upgradedDef;
    public TextMeshProUGUI upgradedHealth;

    [Header("Starting Base Stats")]
    [Tooltip("The starting base attack value (before any upgrades)")]
    public double startingBaseAttack = 10f;
    [Tooltip("The starting base defense value (before any upgrades)")]
    public double startingBaseDefense = 5f;
    [Tooltip("The starting base health value (before any upgrades)")]
    public double startingBaseHealth = 100f;

    private void Start()
    {
        StartCoroutine(CheckForPlayer());
        UpdateCostTexts();
        SetEvents();
    }

    void SetEvents()
    {
        attackUpgradeButton.GetComponent<HoldClickableButton>().OnClicked += UpgradeAttack;
        defenseUpgradeButton.GetComponent<HoldClickableButton>().OnClicked += UpgradeDefense;
        healthUpgradeButton.GetComponent<HoldClickableButton>().OnClicked += UpgradeHealth;
        attackUpgradeButton.GetComponent<HoldClickableButton>().OnHoldClicked += UpgradeAttack;
        defenseUpgradeButton.GetComponent<HoldClickableButton>().OnHoldClicked += UpgradeDefense;
        healthUpgradeButton.GetComponent<HoldClickableButton>().OnHoldClicked += UpgradeHealth;
    }

    IEnumerator CheckForPlayer()
    {
        // If player stats not found, try to find it
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();

            // If found, update UI and subscribe to events
            if (playerStats != null)
            {
                UpdateStatsUI();
                playerStats.OnStatsUpdated += UpdateStatsUI;
            }
        }

        yield return new WaitForSeconds(0.5f);

        // If still not found, keep checking
        if (playerStats == null)
        {
            StartCoroutine(CheckForPlayer());
        }
    }

    // Calculate upgrade counts based on current stats vs starting stats
    private int GetUpgradeCount(StatType statType)
    {
        if (playerStats == null) return 0;
        switch (statType)
        {
            case StatType.Attack:
                return (int)Math.Floor((playerStats.baseAttack - startingBaseAttack) / attackUpgradeAmount);
            case StatType.Defense:
                return (int)Math.Floor((playerStats.baseDefense - startingBaseDefense) / defenseUpgradeAmount);
            case StatType.Health:
                return (int)Math.Floor((playerStats.baseHealth - startingBaseHealth) / healthUpgradeAmount);
            default:
                return 0;
        }
    }

    // Update UI to show current stat values
    private void UpdateStatsUI()
    {
        if (playerStats == null) return;

        if (baseAtkText != null) baseAtkText.text = BigNumberFormatter.Format(playerStats.baseAttack, 0);
        if (baseDefText != null) baseDefText.text = BigNumberFormatter.Format(playerStats.baseDefense, 0);
        if (baseHealthText != null) baseHealthText.text = BigNumberFormatter.Format(playerStats.baseHealth, 0);

        if (upgradedAtk != null) upgradedAtk.text = BigNumberFormatter.Format(playerStats.baseAttack + attackUpgradeAmount, 0);
        if (upgradedDef != null) upgradedDef.text = BigNumberFormatter.Format(playerStats.baseDefense + defenseUpgradeAmount, 0);
        if (upgradedHealth != null) upgradedHealth.text = BigNumberFormatter.Format(playerStats.baseHealth + healthUpgradeAmount, 0);

        UpdateCostTexts();

        // Update button interactability based on gold
        UpdateButtonStates();
    }

    // Calculate cost based on the number of upgrades, not stat value
    private float CalculateUpgradeCost(StatType statType)
    {
        int upgradeCount = GetUpgradeCount(statType);

        // Formula: BaseCost + (UpgradeCount * CostIncreasePerLevel)^Exponent
        float cost = baseCost + Mathf.Pow(upgradeCount * costIncreasePerLevel, costExponent);

        // Ensure cost is at least the minimum value
        return Mathf.Max(cost, minCost);
    }

    // Update cost texts
    private void UpdateCostTexts()
    {
        float attackCost = CalculateUpgradeCost(StatType.Attack);
        float defenseCost = CalculateUpgradeCost(StatType.Defense);
        float healthCost = CalculateUpgradeCost(StatType.Health);

        if (attackCostText != null) attackCostText.text = BigNumberFormatter.Format(attackCost, 0);
        if (defenseCostText != null) defenseCostText.text = BigNumberFormatter.Format(defenseCost, 0);
        if (healthCostText != null) healthCostText.text = BigNumberFormatter.Format(healthCost, 0);
    }

    // Update which buttons are interactable based on available gold
    private void UpdateButtonStates()
    {
        if (playerStats == null) return;

        float attackCost = CalculateUpgradeCost(StatType.Attack);
        float defenseCost = CalculateUpgradeCost(StatType.Defense);
        float healthCost = CalculateUpgradeCost(StatType.Health);

        if (attackUpgradeButton != null)
            attackUpgradeButton.interactable = playerStats.CanAfford(attackCost);

        if (defenseUpgradeButton != null)
            defenseUpgradeButton.interactable = playerStats.CanAfford(defenseCost);

        if (healthUpgradeButton != null)
            healthUpgradeButton.interactable = playerStats.CanAfford(healthCost);
    }

    // Methods to be called by UI buttons
    public void UpgradeAttack()
    {
        if (playerStats == null) return;

        float cost = CalculateUpgradeCost(StatType.Attack);
        if (playerStats.CanAfford(cost))
        {
            playerStats.RemoveGold(Mathf.FloorToInt(cost));
            playerStats.AddBaseStat(StatType.Attack, attackUpgradeAmount);
            UpdateStatsUI();
        }
    }

    public void UpgradeDefense()
    {
        if (playerStats == null) return;

        float cost = CalculateUpgradeCost(StatType.Defense);
        if (playerStats.CanAfford(cost))
        {
            playerStats.RemoveGold(Mathf.FloorToInt(cost));
            playerStats.AddBaseStat(StatType.Defense, defenseUpgradeAmount);
            UpdateStatsUI();
        }
    }

    public void UpgradeHealth()
    {
        if (playerStats == null) return;

        float cost = CalculateUpgradeCost(StatType.Health);
        if (playerStats.CanAfford(cost))
        {
            playerStats.RemoveGold(Mathf.FloorToInt(cost));
            playerStats.AddBaseStat(StatType.Health, healthUpgradeAmount);
            UpdateStatsUI();
        }
    }
}