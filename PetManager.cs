using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class PetManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform invContainer;
    public GameObject container;
    public GameObject dividerContainer;
    public Transform unlockedContainer;

    [Header("Pet Database")]
    public PetData[] allPetData;
    public Image[] petRarityFrames;
    public GameObject defaultFramePrefab;

    [Header("Locked Pets UI")]
    public GameObject dividerPrefab;
    public GameObject lockedPetPrefab;
    public string dividerText = "--- Locked Pets ---";

    [Header("Equipped Pets UI")]
    public PetSlot[] petSlots;

    [Header("Pet Management")]
    public List<OwnedPet> ownedPets = new List<OwnedPet>();
    private string[] equippedPets = new string[3];

    public GameManager gameManager;

    void Awake()
    {
        allPetData = gameManager.petDatabase;
        LoadPets();
        LoadEquippedPets();
    }

    private void Start()
    {
        CreatePetsList();
        InitializeEquippedPetsUI();
        InitializeEquippedPetsInGame();
        gameObject.SetActive(false);
    }

    #region Save/Load System
    void LoadPets()
    {
        string savedData = PlayerPrefs.GetString("Pets", "");
        if (string.IsNullOrEmpty(savedData))
        {
            return;
        }
        string[] allPets = savedData.Split(';');
        ownedPets.Clear();
        foreach (string onePet in allPets)
        {
            if (string.IsNullOrEmpty(onePet)) continue;
            string[] petInfo = onePet.Split(',');
            if (petInfo.Length >= 2)
            {
                OwnedPet ownedPet = new OwnedPet
                {
                    petName = petInfo[0],
                    level = int.Parse(petInfo[1])
                };
                ownedPets.Add(ownedPet);
            }
        }
    }

    void LoadEquippedPets()
    {
        for (int i = 0; i < 3; i++)
        {
            equippedPets[i] = PlayerPrefs.GetString($"EquippedPet_{i}", "");
        }
    }

    void SavePets()
    {
        string dataToSave = string.Join(";", ownedPets.Select(pet => $"{pet.petName},{pet.level}"));
        PlayerPrefs.SetString("Pets", dataToSave);
        PlayerPrefs.Save();
    }

    void SaveEquippedPets()
    {
        for (int i = 0; i < 3; i++)
        {
            PlayerPrefs.SetString($"EquippedPet_{i}", equippedPets[i] ?? "");
        }
        PlayerPrefs.Save();
    }
    #endregion

    #region Pet Equipment System
    public bool EquipPet(string petName, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3)
        {
            return false;
        }
        if (!IsPetOwned(petName))
        {
            return false;
        }
        if (petSlots == null || slotIndex >= petSlots.Length || petSlots[slotIndex] == null)
        {
            return false;
        }
        for (int i = 0; i < 3; i++)
        {
            if (i != slotIndex && equippedPets[i] == petName)
            {
                UnequipPet(i);
                break;
            }
        }
        if (!string.IsNullOrEmpty(equippedPets[slotIndex]))
        {
            UnequipPet(slotIndex);
        }
        equippedPets[slotIndex] = petName;
        SaveEquippedPets();
        CreateEquippedPetUI(petName, slotIndex);
        SpawnPetInGame(petName, slotIndex);
        CreatePetsList();
        return true;
    }

    public void UnequipPet(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3)
        {
            return;
        }
        string previousPet = equippedPets[slotIndex];
        if (string.IsNullOrEmpty(previousPet))
        {
            return;
        }
        equippedPets[slotIndex] = "";
        SaveEquippedPets();
        gameManager?.RemovePet(slotIndex);
        if (petSlots != null && slotIndex < petSlots.Length && petSlots[slotIndex] != null)
        {
            petSlots[slotIndex].RemoveItem();
        }
        CreatePetsList();
    }

    private void SpawnPetInGame(string petName, int slotIndex)
    {
        PetData petData = GetPetData(petName);
        if (petData?.petPrefab != null)
        {
            if (gameManager != null)
            {
                gameManager.InstantiatePet(slotIndex, petData.petPrefab);
            }
        }
    }

    public void InitializeEquippedPetsUI()
    {
        if (petSlots == null)
        {
            return;
        }
        for (int i = 0; i < petSlots.Length && i < 3; i++)
        {
            if (petSlots[i] == null)
            {
                continue;
            }
            if (!string.IsNullOrEmpty(equippedPets[i]))
            {
                CreateEquippedPetUI(equippedPets[i], i);
            }
        }
    }

    public void InitializeEquippedPetsInGame()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                return;
            }
        }
        for (int i = 0; i < 3; i++)
        {
            if (!string.IsNullOrEmpty(equippedPets[i]))
            {
                SpawnPetInGame(equippedPets[i], i);
            }
        }
    }

    private void CreateEquippedPetUI(string petName, int slotIndex)
    {
        if (petSlots == null || slotIndex >= petSlots.Length || petSlots[slotIndex] == null)
        {
            return;
        }
        PetData petData = GetPetData(petName);
        if (petData == null)
        {
            return;
        }
        petSlots[slotIndex].RemoveItem();
        GameObject petUI = Instantiate(defaultFramePrefab, petSlots[slotIndex].transform);
        DragDrop dragDrop = petUI.GetComponent<DragDrop>();
        if (dragDrop != null)
        {
            dragDrop.canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            dragDrop.petName = petName;
            dragDrop.SetCurrentSlot(petSlots[slotIndex]);
        }
        SetPetFrame(petUI, petData.rarity);
        Transform petImageTransform = petUI.transform.Find("Pet Image");
        if (petImageTransform != null)
        {
            Image petImage = petImageTransform.GetComponent<Image>();
            if (petImage != null && petData.petSprite != null)
            {
                petImage.sprite = petData.petSprite;
                petImage.color = Color.white;
            }
        }
        petSlots[slotIndex].currentItem = petUI;
    }
    #endregion

    #region Pet Inventory Management
    public void AddPet(string petName)
    {
        PetData petData = GetPetData(petName);
        if (petData == null)
        {
            return;
        }
        if (IsPetOwned(petName))
        {
            return;
        }
        OwnedPet newOwnedPet = new OwnedPet
        {
            petName = petName,
            level = 1
        };
        ownedPets.Add(newOwnedPet);
        SavePets();
        CreatePetsList();
    }

    public void RemovePet(string petName)
    {
        for (int i = 0; i < 3; i++)
        {
            if (equippedPets[i] == petName)
            {
                UnequipPet(i);
            }
        }
        int removedCount = ownedPets.RemoveAll(pet => pet.petName == petName);
        if (removedCount > 0)
        {
            SavePets();
            CreatePetsList();
        }
    }

    public void UpgradePet(string petName)
    {
        OwnedPet petToUpgrade = ownedPets.FirstOrDefault(pet => pet.petName == petName);
        if (petToUpgrade != null)
        {
            petToUpgrade.level++;
            SavePets();
            UpdateEquippedPetStats(petName);
        }
    }

    private void UpdateEquippedPetStats(string petName)
    {
        for (int i = 0; i < 3; i++)
        {
            if (equippedPets[i] == petName)
            {
                gameManager?.RefreshPetStats(i);
            }
        }
    }
    #endregion

    #region Data Access Methods
    public PetData GetPetData(string petName)
    {
        return allPetData.FirstOrDefault(data => data.petName == petName);
    }

    public OwnedPet GetOwnedPet(string petName)
    {
        return ownedPets.FirstOrDefault(pet => pet.petName == petName);
    }

    public string GetEquippedPet(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 3)
            return equippedPets[slotIndex] ?? "";
        return "";
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 3)
            return string.IsNullOrEmpty(equippedPets[slotIndex]);
        return true;
    }

    public bool IsPetOwned(string petName)
    {
        return ownedPets.Any(pet => pet.petName == petName);
    }

    public bool IsPetEquipped(string petName)
    {
        return equippedPets.Contains(petName);
    }

    public List<OwnedPet> GetOwnedPets()
    {
        return new List<OwnedPet>(ownedPets);
    }

    public List<PetData> GetLockedPets()
    {
        return allPetData.Where(petData => !IsPetOwned(petData.petName)).ToList();
    }
    #endregion

    #region UI Creation
    public void CreatePetsList()
    {
        foreach (Transform child in invContainer)
        {
            Destroy(child.gameObject);
        }
        CreateUnequippedPetsSection();
        List<PetData> lockedPets = GetLockedPets();
        if (lockedPets.Count > 0)
        {
            CreateDivider();
            CreateLockedPetsSection(lockedPets);
        }
    }

    private void CreateUnequippedPetsSection()
    {
        List<OwnedPet> unequippedPets = ownedPets.Where(pet => !IsPetEquipped(pet.petName)).ToList();
        if (unequippedPets.Count == 0)
        {
            return;
        }
        GameObject unlockedParent = Instantiate(container, invContainer);
        unlockedContainer = unlockedParent.transform;
        foreach (OwnedPet pet in unequippedPets)
        {
            CreateOwnedPetUI(pet, unlockedParent.transform);
        }
    }

    private void CreateLockedPetsSection(List<PetData> lockedPets)
    {
        GameObject lockedParent = Instantiate(container, invContainer);
        foreach (PetData lockedPet in lockedPets)
        {
            CreateLockedPetUI(lockedPet, lockedParent.transform);
        }
    }

    private void CreateOwnedPetUI(OwnedPet pet, Transform parent)
    {
        PetData petData = GetPetData(pet.petName);
        if (petData == null)
        {
            return;
        }
        GameObject petUI = Instantiate(defaultFramePrefab, parent);
        SetPetFrame(petUI, petData.rarity);
        DragDrop dragDrop = petUI.GetComponent<DragDrop>();
        if (dragDrop != null)
        {
            dragDrop.canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            dragDrop.petName = pet.petName;
            dragDrop.SetCurrentSlot(null);
        }
        Transform petImageTransform = petUI.transform.Find("Pet Image");
        if (petImageTransform != null)
        {
            Image petImage = petImageTransform.GetComponent<Image>();
            if (petImage != null && petData.petSprite != null)
            {
                petImage.sprite = petData.petSprite;
                petImage.color = Color.white;
            }
        }
    }

    private void CreateLockedPetUI(PetData petData, Transform parent)
    {
        GameObject prefabToUse = lockedPetPrefab != null ? lockedPetPrefab : defaultFramePrefab;
        GameObject petUI = Instantiate(prefabToUse, parent);
        DragDrop dragDrop = petUI.GetComponent<DragDrop>();
        if (dragDrop != null)
        {
            dragDrop.enabled = false;
        }
        SetPetFrame(petUI, petData.rarity);
        Transform petImageTransform = petUI.transform.Find("Pet Image");
        if (petImageTransform != null)
        {
            Image petImage = petImageTransform.GetComponent<Image>();
            if (petImage != null && petData.petSprite != null)
            {
                petImage.sprite = petData.petSprite;
                petImage.color = Color.gray;
            }
        }
        Image frameImage = petUI.GetComponent<Image>();
        if (frameImage != null)
        {
            Color frameColor = frameImage.color;
            frameColor.a = 0.5f;
            frameImage.color = frameColor;
        }
    }

    private void CreateDivider()
    {
        if (dividerPrefab == null || dividerContainer == null) return;
        GameObject parent = Instantiate(dividerContainer, invContainer);
        GameObject divider = Instantiate(dividerPrefab, parent.transform);
        Text dividerTextComponent = divider.GetComponent<Text>();
        if (dividerTextComponent != null)
        {
            dividerTextComponent.text = dividerText;
        }
        RectTransform parentRect = parent.GetComponent<RectTransform>();
        if (parentRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    private void SetPetFrame(GameObject petUI, Rarity rarity)
    {
        if (petRarityFrames == null || petRarityFrames.Length == 0) return;
        Image frameImage = petUI.GetComponent<Image>();
        if (frameImage == null) return;
        int rarityIndex = (int)rarity;
        if (rarityIndex >= 0 && rarityIndex < petRarityFrames.Length && petRarityFrames[rarityIndex] != null)
        {
            frameImage.sprite = petRarityFrames[rarityIndex].sprite;
            frameImage.color = petRarityFrames[rarityIndex].color;
            frameImage.material = petRarityFrames[rarityIndex].material;
        }
    }
    #endregion
}

[System.Serializable]
public class OwnedPet
{
    public string petName;
    public int level;
}