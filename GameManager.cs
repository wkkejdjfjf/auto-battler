using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public List<Character> allies;
    public List<Character> enemies;

    [Header("Player spawn settings")]
    public Character player;
    public Transform playerPosition;

    [Header("Pet Spawn settings")]
    public Positions[] petPositions;

    [Header("Databases")]
    public PetData[] petDatabase;
    public Ability[] abilityDatabase;

    private PetManager petManager;

    private void Start()
    {
        petManager = FindFirstObjectByType<PetManager>();
        InstantiateCharacter();
    }

    public void InstantiateCharacter()
    {
        Instantiate(player.gameObject, playerPosition.position, playerPosition.rotation);
    }

    public void InstantiatePet(int index, GameObject petPrefab)
    {
        if (petManager == null)
        {
            petManager = FindFirstObjectByType<PetManager>();
        }
        string equippedName = petManager != null ? petManager.GetEquippedPet(index) : "<unknown>";

        if (petPositions == null || petPositions.Length == 0)
        {
            Debug.LogError("petPositions array is null or empty. Assign positions in the inspector.");
            return;
        }
        if (index < 0 || index >= petPositions.Length)
        {
            Debug.LogError($"Invalid pet position index: {index}");
            return;
        }
        if (petPositions[index] == null)
        {
            Debug.LogError($"petPositions[{index}] is null. Check your array assignments.");
            return;
        }
        if (petPositions[index].transform == null)
        {
            Debug.LogError($"petPositions[{index}] has no transform. Ensure it's a valid scene object.");
            return;
        }
        if (petPrefab == null)
        {
            Debug.LogError($"Cannot instantiate pet in slot {index}. Prefab is NULL for equipped '{equippedName}'. Check PetData.petPrefab.");
            return;
        }

        if (petPositions[index].occupied && petPositions[index].obj != null)
        {
            Destroy(petPositions[index].obj);
            petPositions[index].obj = null;
            petPositions[index].occupied = false;
        }

        Transform parentTransform = petPositions[index].transform;
        GameObject instantiatedPet = Instantiate(petPrefab, parentTransform.position, parentTransform.rotation);
        instantiatedPet.transform.SetParent(parentTransform);
        instantiatedPet.transform.localPosition = Vector3.zero;
        instantiatedPet.transform.localRotation = Quaternion.identity;
        instantiatedPet.name = string.IsNullOrEmpty(equippedName) ? petPrefab.name : equippedName;
        instantiatedPet.SetActive(true);

        ConfigurePetComponent(instantiatedPet, index);

        petPositions[index].obj = instantiatedPet;
        petPositions[index].occupied = true;

        var cooldownHandler = FindFirstObjectByType<CooldownHandler>();
        if (cooldownHandler != null)
        {
            var petComponent = instantiatedPet.GetComponent<Pet>();
            if (petComponent != null)
            {
                cooldownHandler.SetPetInSlot(petComponent, index);
            }
        }
    }

    private void ConfigurePetComponent(GameObject petObject, int slotIndex)
    {
        if (petManager == null)
        {
            Debug.LogWarning("PetManager not found, cannot configure pet component");
            return;
        }

        string petName = petManager.GetEquippedPet(slotIndex);
        if (string.IsNullOrEmpty(petName))
        {
            Debug.LogWarning($"No pet equipped in slot {slotIndex}");
            return;
        }

        PetData petData = petManager.GetPetData(petName);
        OwnedPet ownedPet = petManager.GetOwnedPet(petName);

        if (petData == null)
        {
            Debug.LogError($"Pet data not found for: {petName}");
            return;
        }
        if (ownedPet == null)
        {
            Debug.LogError($"Owned pet data not found for: {petName}");
            return;
        }

        Pet petComponent = petObject.GetComponent<Pet>();
        if (petComponent != null)
        {
            petComponent.id = petData.petName;
            petComponent.level = ownedPet.level;
            petComponent.rarity = petData.rarity;
            petComponent.ability = petData.specialAbility;
            petComponent.abilityCooldown = petData.specialAbility != null ? petData.specialAbility.cooldownTime : 0f;

            Debug.Log($"Configured pet {petName}: Level {ownedPet.level}, Ability: {petData.specialAbility?.name ?? "None"}");
        }
        else
        {
            Debug.LogError($"Pet prefab {petName} doesn't have a Pet component!");
        }
    }

    public void RemovePet(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < petPositions.Length)
        {
            if (petPositions[slotIndex].occupied && petPositions[slotIndex].obj != null)
            {
                Destroy(petPositions[slotIndex].obj);
                petPositions[slotIndex].obj = null;
                petPositions[slotIndex].occupied = false;

                var cooldownHandler = FindFirstObjectByType<CooldownHandler>();
                if (cooldownHandler != null)
                {
                    cooldownHandler.RemovePetFromSlot(slotIndex);
                }
            }
        }
    }

    public void RefreshPetStats(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < petPositions.Length && petPositions[slotIndex].obj != null)
        {
            ConfigurePetComponent(petPositions[slotIndex].obj, slotIndex);
        }
    }

    public GameObject GetPetInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < petPositions.Length && petPositions[slotIndex].occupied)
        {
            return petPositions[slotIndex].obj;
        }
        return null;
    }

    public bool IsPetSlotOccupied(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < petPositions.Length)
        {
            return petPositions[slotIndex].occupied && petPositions[slotIndex].obj != null;
        }
        return false;
    }
}