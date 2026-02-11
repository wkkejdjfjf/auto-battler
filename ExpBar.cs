using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

public class ExpBar : MonoBehaviour
{
    [SerializeField] float currentExp;
    [SerializeField] float maxExp;
    [SerializeField] int level;
    [SerializeField] Image fillImage;
    [SerializeField] TextMeshProUGUI currentExpText;
    [SerializeField] TextMeshProUGUI maxExpText;
    [SerializeField] TextMeshProUGUI levelText;
    private LevelSystem levelSystem;

    private void Awake()
    {
        levelSystem = FindFirstObjectByType<LevelSystem>();
    }

    private void OnEnable()
    {
        levelSystem.OnExpGained.AddListener(UpdateExpUI);
        levelSystem.OnLevelUp.AddListener(UpdateLevelUI);
    }

    private void UpdateExpUI(double currentExp, double maxExp)
    {
        fillImage.fillAmount = (float)(currentExp / maxExp);
        currentExpText.text = BigNumberFormatter.Format(currentExp, 0); // No decimals for exp
        maxExpText.text = BigNumberFormatter.Format(maxExp, 0); // No decimals for exp
    }

    private void UpdateLevelUI(int level)
    {
        levelText.text = level.ToString();
    }
}