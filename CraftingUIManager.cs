using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CraftingUIManager : MonoBehaviour
{
    [Header("References")]
    public CraftingSystem craftingSystem;

    [Header("Fragment Display")]
    public TMP_Text commonFragmentText;
    public TMP_Text rareFragmentText;
    public TMP_Text epicFragmentText;
    public TMP_Text legendaryFragmentText;
    public TMP_Text mythicalFragmentText;

    [Header("Fragment Cost Display")]
    public TMP_Text commonCostText;
    public TMP_Text rareCostText;
    public TMP_Text epicCostText;
    public TMP_Text legendaryCostText;
    public TMP_Text mythicalCostText;

    [Header("Collection Progress")]
    public TMP_Text commonProgressText;
    public TMP_Text rareProgressText;
    public TMP_Text epicProgressText;
    public TMP_Text legendaryProgressText;
    public TMP_Text mythicalProgressText;

    [Header("Craft Result")]
    public GameObject weaponCraftingPanel;
    public Transform craftingResultContainer;
    public GameObject weaponCraftResultPrefab;
    public TMP_Text craftResultMessageText;

    [Header("Weapon Collection Panel")]
    public Transform weaponCollectionContainer;
    public GameObject weaponCollectionItemPrefab;

    private Dictionary<string, GameObject> collectionItems = new Dictionary<string, GameObject>();

    private bool isUpdatingUI = false;
    private const float UI_UPDATE_INTERVAL = 2.0f;

    private static readonly int[] FRAGMENT_COSTS = { 50, 75, 100, 150, 200 };
    private static readonly string[] FRAGMENT_IDS =
    {
        "CommonFragment",
        "RareFragment",
        "EpicFragment",
        "LegendaryFragment",
        "MythicalFragment"
    };

    void Start()
    {
        craftingSystem.OnInventoryChanged += OnInventoryChangedHandler;
        craftingSystem.OnResourceUnlocked += OnResourceUnlockedHandler;

        RefreshFragmentCounts();
        RefreshFragmentCosts();
        RefreshCollectionProgress();
        RefreshWeaponCollection();

        StartCoroutine(PeriodicUIUpdate());
    }

    void OnDestroy()
    {
        if (craftingSystem != null)
        {
            craftingSystem.OnInventoryChanged -= OnInventoryChangedHandler;
            craftingSystem.OnResourceUnlocked -= OnResourceUnlockedHandler;
        }

        StopAllCoroutines();
        collectionItems.Clear();
    }

    private void OnInventoryChangedHandler()
    {
        if (!isUpdatingUI)
            StartCoroutine(ThrottledUIUpdate());
    }

    private void OnResourceUnlockedHandler(string resourceId)
    {
        RefreshFragmentCounts();
        RefreshCollectionProgress();
    }

    private IEnumerator ThrottledUIUpdate()
    {
        isUpdatingUI = true;
        yield return new WaitForSeconds(0.1f);

        RefreshFragmentCounts();
        RefreshCollectionProgress();
        RefreshWeaponCollectionValues();

        isUpdatingUI = false;
    }

    private IEnumerator PeriodicUIUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(UI_UPDATE_INTERVAL);

            if (!isUpdatingUI)
            {
                RefreshFragmentCounts();
                RefreshCollectionProgress();
                RefreshWeaponCollectionValues();
            }
        }
    }

    // ─── Fragment Count Display ───────────────────────────────────────────────

    private void RefreshFragmentCounts()
    {
        SetFragmentText(commonFragmentText, "CommonFragment");
        SetFragmentText(rareFragmentText, "RareFragment");
        SetFragmentText(epicFragmentText, "EpicFragment");
        SetFragmentText(legendaryFragmentText, "LegendaryFragment");
        SetFragmentText(mythicalFragmentText, "MythicalFragment");
    }

    private void SetFragmentText(TMP_Text label, string fragmentId)
    {
        if (label == null) return;
        label.text = BigNumberFormatter.Format(craftingSystem.GetResourceAmount(fragmentId), 0);
    }

    private void RefreshFragmentCosts()
    {
        if (commonCostText != null) commonCostText.text = $"{FRAGMENT_COSTS[0]}";
        if (rareCostText != null) rareCostText.text = $"{FRAGMENT_COSTS[1]}";
        if (epicCostText != null) epicCostText.text = $"{FRAGMENT_COSTS[2]}";
        if (legendaryCostText != null) legendaryCostText.text = $"{FRAGMENT_COSTS[3]}";
        if (mythicalCostText != null) mythicalCostText.text = $"{FRAGMENT_COSTS[4]}";
    }

    // ─── Collection Progress ─────────────────────────────────────────────────

    private void RefreshCollectionProgress()
    {
        SetProgressText(commonProgressText, Rarity.Common);
        SetProgressText(rareProgressText, Rarity.Rare);
        SetProgressText(epicProgressText, Rarity.Epic);
        SetProgressText(legendaryProgressText, Rarity.Legendary);
        SetProgressText(mythicalProgressText, Rarity.Mythical);
    }

    private void SetProgressText(TMP_Text label, Rarity rarity)
    {
        if (label == null) return;
        label.text = craftingSystem.GetCollectionProgressText(rarity);
    }

    // ─── Weapon Collection Panel ─────────────────────────────────────────────

    private void RefreshWeaponCollection()
    {
        if (weaponCollectionContainer == null || weaponCollectionItemPrefab == null) return;

        foreach (Transform child in weaponCollectionContainer)
            Destroy(child.gameObject);

        collectionItems.Clear();

        foreach (var pair in craftingSystem.craftedItemsData)
        {
            string itemId = pair.Key;
            ItemData itemData = pair.Value;

            CraftableItemSO weapon = craftingSystem.GetCraftableItemData(itemId);
            if (weapon == null || !weapon.canSocketToSoulWeapon) continue;

            CreateCollectionItemUI(itemId, weapon, itemData);
        }
    }

    private void CreateCollectionItemUI(string itemId, CraftableItemSO weapon, ItemData itemData)
    {
        GameObject item = Instantiate(weaponCollectionItemPrefab, weaponCollectionContainer);
        item.name = "WeaponCollect_" + itemId;

        TMP_Text nameText = item.transform.Find("NameText").GetComponent<TMP_Text>();
        TMP_Text rarityText = item.transform.Find("RarityText").GetComponent<TMP_Text>();
        TMP_Text levelText = item.transform.Find("LevelText").GetComponent<TMP_Text>();
        TMP_Text bonusText = item.transform.Find("BonusText").GetComponent<TMP_Text>();
        Image iconImage = item.transform.Find("IconImage").GetComponent<Image>();
        Button upgradeBtn = item.transform.Find("UpgradeButton").GetComponent<Button>();
        Image levelBarFill = item.transform.Find("LevelBarFill").GetComponent<Image>();

        if (nameText != null) nameText.text = weapon.displayName;
        if (rarityText != null) rarityText.text = weapon.rarity.ToString();
        if (levelText != null) levelText.text = $"Lv {itemData.level}/{weapon.maxLevel}";
        if (bonusText != null) bonusText.text = weapon.GetCompleteStatDescription(itemData.level);

        if (iconImage != null)
        {
            if (weapon.icon != null)
            {
                iconImage.sprite = weapon.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        if (levelBarFill != null)
        {
            float progress = itemData.copiesForNextLevel > 0
                ? Mathf.Clamp01((float)itemData.count / itemData.copiesForNextLevel)
                : 1f;
            levelBarFill.fillAmount = progress;
        }

        if (upgradeBtn != null)
        {
            if (itemData.level < weapon.maxLevel)
            {
                upgradeBtn.gameObject.SetActive(true);
                upgradeBtn.interactable = craftingSystem.CanLevelUp(itemId);

                string capturedId = itemId;
                upgradeBtn.onClick.RemoveAllListeners();
                upgradeBtn.onClick.AddListener(() => TryLevelUp(capturedId));

                HoldClickableButton holdBtn = upgradeBtn.GetComponent<HoldClickableButton>();
                if (holdBtn != null)
                {
                    System.Action action = () => TryLevelUp(capturedId);
                    holdBtn.OnClicked += action;
                    holdBtn.OnHoldClicked += action;
                    holdBtn._debugMode = false;
                }
            }
            else
            {
                upgradeBtn.gameObject.SetActive(false);
            }
        }

        collectionItems[itemId] = item;
    }

    private void RefreshWeaponCollectionValues()
    {
        if (weaponCollectionContainer == null) return;

        foreach (var pair in craftingSystem.craftedItemsData)
        {
            string itemId = pair.Key;
            ItemData itemData = pair.Value;

            CraftableItemSO weapon = craftingSystem.GetCraftableItemData(itemId);
            if (weapon == null || !weapon.canSocketToSoulWeapon) continue;

            if (!collectionItems.TryGetValue(itemId, out GameObject item))
            {
                CreateCollectionItemUI(itemId, weapon, itemData);
                continue;
            }

            TMP_Text levelText = item.transform.Find("LevelText").GetComponent<TMP_Text>();
            TMP_Text bonusText = item.transform.Find("BonusText").GetComponent<TMP_Text>();
            Image levelBarFill = item.transform.Find("LevelBarFill").GetComponent<Image>();
            Button upgradeBtn = item.transform.Find("UpgradeButton").GetComponent<Button>();

            if (levelText != null) levelText.text = $"Lv {itemData.level}/{weapon.maxLevel}";
            if (bonusText != null) bonusText.text = weapon.GetCompleteStatDescription(itemData.level);

            if (levelBarFill != null)
            {
                float progress = itemData.copiesForNextLevel > 0
                    ? Mathf.Clamp01((float)itemData.count / itemData.copiesForNextLevel)
                    : 1f;
                levelBarFill.fillAmount = progress;
            }

            if (upgradeBtn != null)
            {
                if (itemData.level < weapon.maxLevel)
                {
                    upgradeBtn.gameObject.SetActive(true);
                    upgradeBtn.interactable = craftingSystem.CanLevelUp(itemId);
                }
                else
                {
                    upgradeBtn.gameObject.SetActive(false);
                }
            }
        }
    }

    // ─── Level Up ────────────────────────────────────────────────────────────

    private void TryLevelUp(string itemId)
    {
        if (craftingSystem.LevelUpItem(itemId))
        {
            if (!isUpdatingUI)
                StartCoroutine(ThrottledUIUpdate());
        }
    }

    // ─── Craft Buttons (called from Unity UI) ────────────────────────────────

    public void OnCraftWeaponClicked(int rarityIndex)
    {
        Rarity rarity = (Rarity)rarityIndex;
        StartCoroutine(CraftWeaponAnimation(rarity));
    }

    public void OnCraftMultipleClicked(int rarityIndex)
    {
        Rarity rarity = (Rarity)rarityIndex;
        StartCoroutine(CraftMultipleAnimation(rarity, 10));
    }

    // ─── Craft Animations ────────────────────────────────────────────────────

    private IEnumerator CraftWeaponAnimation(Rarity rarity)
    {
        if (craftingResultContainer != null)
        {
            foreach (Transform child in craftingResultContainer)
                Destroy(child.gameObject);
        }

        if (craftResultMessageText != null)
            craftResultMessageText.text = "Crafting...";

        yield return new WaitForSeconds(1.5f);

        CraftableItemSO craftedWeapon = craftingSystem.CraftRandomWeapon(rarity);

        if (craftedWeapon == null)
        {
            if (craftResultMessageText != null)
                craftResultMessageText.text = $"Not enough {rarity} Fragments! Need {FRAGMENT_COSTS[(int)rarity]}.";
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        bool isDuplicate = craftingSystem.craftedItemsData.TryGetValue(craftedWeapon.itemId, out ItemData data)
                           && data.count > 1;

        if (craftResultMessageText != null)
        {
            craftResultMessageText.text = isDuplicate
                ? $"Duplicate! Converted to Soul Essence."
                : $"NEW weapon unlocked!";
        }

        DisplayCraftedWeapon(craftedWeapon, isDuplicate);

        yield return new WaitForSeconds(2f);
    }

    private IEnumerator CraftMultipleAnimation(Rarity rarity, int count)
    {
        if (craftingResultContainer != null)
        {
            foreach (Transform child in craftingResultContainer)
                Destroy(child.gameObject);
        }

        if (craftResultMessageText != null)
            craftResultMessageText.text = $"Crafting x{count}...";

        List<CraftableItemSO> craftedWeapons = craftingSystem.CraftMultipleWeapons(rarity, count);

        if (craftedWeapons.Count == 0)
        {
            if (craftResultMessageText != null)
                craftResultMessageText.text = $"Not enough {rarity} Fragments! Need {FRAGMENT_COSTS[(int)rarity]}.";
            yield break;
        }

        foreach (var weapon in craftedWeapons)
        {
            bool isDuplicate = craftingSystem.craftedItemsData.TryGetValue(weapon.itemId, out ItemData data)
                               && data.count > 1;
            DisplayCraftedWeapon(weapon, isDuplicate);
            yield return new WaitForSeconds(0.3f);
        }

        if (craftResultMessageText != null)
            craftResultMessageText.text = $"Crafted {craftedWeapons.Count} weapons!";
    }

    private void DisplayCraftedWeapon(CraftableItemSO weapon, bool isDuplicate)
    {
        if (craftingResultContainer == null || weaponCraftResultPrefab == null) return;

        GameObject resultUI = Instantiate(weaponCraftResultPrefab, craftingResultContainer);

        Image weaponIcon = resultUI.transform.Find("WeaponIcon")?.GetComponent<Image>();
        if (weaponIcon != null && weapon.icon != null)
            weaponIcon.sprite = weapon.icon;

        TMP_Text weaponName = resultUI.transform.Find("WeaponName")?.GetComponent<TMP_Text>();
        if (weaponName != null)
            weaponName.text = weapon.displayName;

        TMP_Text duplicateLabel = resultUI.transform.Find("DuplicateLabel")?.GetComponent<TMP_Text>();
        if (duplicateLabel != null)
            duplicateLabel.gameObject.SetActive(isDuplicate);

        Image frame = resultUI.GetComponent<Image>();
        if (frame != null)
            frame.color = GetRarityColor(weapon.rarity);
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    private Color GetRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common: return new Color(0.8f, 0.8f, 0.8f);
            case Rarity.Rare: return new Color(0.3f, 0.8f, 0.3f);
            case Rarity.Epic: return new Color(0.6f, 0.3f, 0.9f);
            case Rarity.Legendary: return new Color(1f, 0.7f, 0.2f);
            case Rarity.Mythical: return new Color(1f, 0.2f, 0.2f);
            default: return Color.white;
        }
    }

    public void AddResource(string resourceId, int amount)
    {
        craftingSystem.AddResource(resourceId, amount);
    }
}