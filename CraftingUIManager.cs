using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CraftingUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CraftingSystem craftingSystem;

    [System.Serializable]
    public class FragmentUIEntry
    {
        public Rarity rarity;
        public TMP_Text amountText;
        public TMP_Text costText;
        public TMP_Text progressText;
        public Button craftButton;
    }

    [Header("Fragment UI")]
    [SerializeField] private List<FragmentUIEntry> fragmentEntries;

    [Header("Weapon Collection")]
    [SerializeField] private Transform weaponCollectionContainer;
    [SerializeField] private WeaponItemUI weaponCollectionItemPrefab;
    [SerializeField] private TextMeshProUGUI soulEssenceText;

    [Header("Craft Result")]
    [SerializeField] private Transform craftingResultContainer;
    [SerializeField] private GameObject weaponCraftResultPrefab;
    [SerializeField] private TMP_Text craftResultMessageText;

    private Dictionary<string, WeaponItemUI> collectionItems = new Dictionary<string, WeaponItemUI>();

    #region Unity Lifecycle

    private void Start()
    {
        if (craftingSystem == null)
        {
            Debug.LogError("CraftingSystem reference missing!");
            return;
        }

        craftingSystem.OnInventoryChanged += RefreshAllUI;
        craftingSystem.OnResourceUnlocked += _ => RefreshFragments();

        RefreshAllUI();
        BuildWeaponCollection();
    }

    private void OnDestroy()
    {
        if (craftingSystem == null) return;

        craftingSystem.OnInventoryChanged -= RefreshAllUI;
    }

    #endregion

    #region Main Refresh

private void RefreshAllUI()
{
    RefreshFragments();
    RefreshWeaponCollection();
    RefreshSoulEssence();
}


    #endregion

    #region Fragment UI

    private void RefreshSoulEssence()
    {
        if (soulEssenceText == null) return;

        int essenceAmount = craftingSystem.GetSoulEssenceAmount();
        soulEssenceText.text = $"Soul Essence: {BigNumberFormatter.Format(essenceAmount, 0)}";
    }


    private void RefreshFragments()
    {
        foreach (var entry in fragmentEntries)
        {
            string fragmentId = GetFragmentId(entry.rarity);
            int amount = craftingSystem.GetResourceAmount(fragmentId);
            int cost = GetFragmentCost(entry.rarity);

            if (entry.amountText != null)
                entry.amountText.text = BigNumberFormatter.Format(amount, 0);

            if (entry.costText != null)
                entry.costText.text = cost.ToString();

            if (entry.progressText != null)
                entry.progressText.text =
                    craftingSystem.GetCollectionProgressText(entry.rarity);

            if (entry.craftButton != null)
                entry.craftButton.interactable = amount >= cost;
        }
    }

    #endregion

    #region Weapon Collection

    private void BuildWeaponCollection()
    {
        foreach (Transform child in weaponCollectionContainer)
            Destroy(child.gameObject);

        collectionItems.Clear();

        foreach (var pair in craftingSystem.GetCraftedItems())
        {
            CreateOrUpdateItem(pair.Key, pair.Value);
        }
    }

    private void RefreshWeaponCollection()
    {
        foreach (var pair in craftingSystem.GetCraftedItems())
        {
            CreateOrUpdateItem(pair.Key, pair.Value);
        }
    }

    private void CreateOrUpdateItem(string itemId, ItemData data)
    {
        CraftableItemSO weapon = craftingSystem.GetCraftableItemData(itemId);
        if (weapon == null || !weapon.canSocketToSoulWeapon)
            return;

        if (!collectionItems.TryGetValue(itemId, out var uiItem))
        {
            uiItem = Instantiate(weaponCollectionItemPrefab, weaponCollectionContainer);
            collectionItems[itemId] = uiItem;
        }

        uiItem.Setup(itemId, weapon, data, craftingSystem);
    }

    #endregion

    #region Crafting

    public void OnCraftWeaponClicked(int rarityIndex)
    {
        Rarity rarity = (Rarity)rarityIndex;
        StartCoroutine(CraftWeaponRoutine(rarity));
    }

    private IEnumerator CraftWeaponRoutine(Rarity rarity)
    {
        ClearCraftResultUI();

        if (craftResultMessageText != null)
            craftResultMessageText.text = "Crafting...";

        yield return new WaitForSeconds(1f);

        CraftableItemSO weapon = craftingSystem.CraftRandomWeapon(rarity);

        if (weapon == null)
        {
            if (craftResultMessageText != null)
                craftResultMessageText.text = "Not enough fragments!";
            yield break;
        }

        bool isDuplicate = craftingSystem.IsDuplicate(weapon.itemId);

        DisplayCraftedWeapon(weapon, isDuplicate);

        if (craftResultMessageText != null)
            craftResultMessageText.text = isDuplicate
                ? "Duplicate! Converted."
                : "NEW Weapon!";
    }

    private void ClearCraftResultUI()
    {
        foreach (Transform child in craftingResultContainer)
            Destroy(child.gameObject);
    }

    private void DisplayCraftedWeapon(CraftableItemSO weapon, bool isDuplicate)
    {
        GameObject resultUI = Instantiate(weaponCraftResultPrefab, craftingResultContainer);

        resultUI.GetComponentInChildren<Image>().sprite = weapon.icon;

        var text = resultUI.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = weapon.displayName;
    }

    #endregion

    #region Utility

    private string GetFragmentId(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return "CommonFragment";
            case Rarity.Rare: return "RareFragment";
            case Rarity.Epic: return "EpicFragment";
            case Rarity.Legendary: return "LegendaryFragment";
            case Rarity.Mythical: return "MythicalFragment";
            default: return "CommonFragment";
        }
    }

    private int GetFragmentCost(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return 50;
            case Rarity.Rare: return 75;
            case Rarity.Epic: return 100;
            case Rarity.Legendary: return 150;
            case Rarity.Mythical: return 200;
            default: return 50;
        }
    }

    #endregion
}
