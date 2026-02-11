using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public GameManager gameManager;
    public AbilityManager abilityManager;
    public PetManager petManager;

    [Header("Temporary Settings")]
    public int abilityShopLevel = 1;
    public int petShopLevel = 1;

    [Header("Gacha Tables")]
    public GachaTableConfig abilityTable;
    public GachaTableConfig petTable;

    [Header("Economy")]
    public int singleRollCost = 10;
    public int tenRollCost = 90;

    [Header("Cross-Table Features")]
    [SerializeField] private bool enableCrossPity = false; // Pity affects all tables
    [SerializeField] private int crossPityThreshold = 50;

    [Header("Display UI")]
    public GameObject resultContainer;
    public Image[] rarityFrames;
    public GameObject defaultGachaItemPrefab;
    public GameObject panel;
    public GameObject panelAbilitySingleRollButton;
    public GameObject panelAbilityMultipleRollButton;
    public GameObject panelPetSingleRollButton;
    public GameObject panelPetMultipleRollButton;

    private PetData[] petDatabase;
    private Ability[] abilityDatabase;
    private GachaLevelConfig abilityLevelConfig;
    private GachaLevelConfig petLevelConfig;

    private void Start()
    {
        petDatabase = gameManager.petDatabase;
        abilityDatabase = gameManager.abilityDatabase;

        InitializeLevelConfig();
        InitializeAbilityGacha();
    }

    void InitializeLevelConfig()
    {
        var abilityIndex = Mathf.Clamp(abilityShopLevel - 1, 0, abilityTable.levelConfigs.Count - 1);
        abilityLevelConfig = abilityTable.levelConfigs[abilityIndex];

        var petIndex = Mathf.Clamp(petShopLevel - 1, 0, petTable.levelConfigs.Count - 1);
        petLevelConfig = petTable.levelConfigs[abilityIndex];
    }

    void InitializeAbilityGacha()
    {
        abilityTable.items.Clear();
        petTable.items.Clear();

        // --- Abilities ---
        var abilityProbMap = abilityLevelConfig.rarities.ToDictionary(r => r.rarity, r => r.probability);
        foreach (var group in abilityDatabase.GroupBy(a => a.rarity))
        {
            float perItemProb = abilityProbMap[group.Key] / group.Count();
            foreach (var a in group)
            {
                GachaItem item = new GachaItem
                {
                    name = a.name,
                    icon = a.image,
                    rarity = a.rarity,
                    probability = perItemProb
                };
                abilityTable.items.Add(item);
            }
        }

        abilityTable.ValidateTable(); // <-- important

        // --- Pets ---
        var petProbMap = petLevelConfig.rarities.ToDictionary(r => r.rarity, r => r.probability);
        foreach (var group in petDatabase.GroupBy(p => p.rarity))
        {
            float perItemProb = petProbMap[group.Key] / group.Count();
            foreach (var p in group)
            {
                GachaItem item = new GachaItem
                {
                    name = p.petName,
                    icon = p.petSprite,
                    rarity = p.rarity,
                    probability = perItemProb
                };
                petTable.items.Add(item);
            }
        }

        petTable.ValidateTable(); // <-- important
    }



    public void AbilityRollSingle()
    {
        panel.SetActive(true);

        panelPetSingleRollButton.SetActive(false);
        panelPetMultipleRollButton.SetActive(false);
        panelAbilitySingleRollButton.SetActive(true);
        panelAbilitySingleRollButton.SetActive(true);

        GachaItem ability= abilityTable.Roll();
        ShowResultSingle(ability);

        abilityManager.AddAbility(ability.name);
        Debug.Log("You got a ability: " + ability.name);
    }
    public void PetRollSingle()
    {
        panel.SetActive(true);

        panelPetSingleRollButton.SetActive(true);
        panelPetMultipleRollButton.SetActive(true);
        panelAbilitySingleRollButton.SetActive(false);
        panelAbilitySingleRollButton.SetActive(false);

        GachaItem pet = petTable.Roll();
        ShowResultSingle(pet);

        petManager.AddPet(pet.name);
        Debug.Log("You got a pet: " +  pet.name);
    }
    public void AbilityRollMultiple(int count)
    {
        panel.SetActive(true);

        panelPetSingleRollButton.SetActive(false);
        panelPetMultipleRollButton.SetActive(false);
        panelAbilitySingleRollButton.SetActive(true);
        panelAbilitySingleRollButton.SetActive(true);

        List<GachaItem> abilities = abilityTable.RollMultiple(count);
    }
    public void PetRollMultiple(int count)
    {
        panel.SetActive(true);

        panelPetSingleRollButton.SetActive(true);
        panelPetMultipleRollButton.SetActive(true);
        panelAbilitySingleRollButton.SetActive(false);
        panelAbilitySingleRollButton.SetActive(false);

        List<GachaItem> pets = petTable.RollMultiple(count);
    }

    void ShowResultSingle(GachaItem item)
    {
        foreach (Transform child in resultContainer.transform)
        {
            Destroy(child.gameObject);
        }
        int rarityIndex = (int)item.rarity;
        GameObject result = Instantiate(defaultGachaItemPrefab, resultContainer.transform);
        Image frame = rarityFrames[rarityIndex];
        Image resultImage = result.GetComponent<Image>();
        resultImage.color = frame.color;
        resultImage.sprite = frame.sprite;
        resultImage.material = frame.material;

        Image image = result.transform.Find("Image").GetComponent<Image>();
        image.sprite = item.icon;
    }
    void ShowResultMultiple(GachaItem[] items)
    {
        foreach (Transform child in resultContainer.transform)
        {
            Destroy(child);
        }
    }
}
