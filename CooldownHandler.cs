using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using Unity.VisualScripting;

public class CooldownHandler : MonoBehaviour
{
    [Header("Player Abilities")]
    public List<Image> images;
    public List<AbilityCooldown> abilityCooldowns;

    [Header("Pet Abilities (3 Pets)")]
    public List<Image> petImages = new List<Image>(3);
    public List<AbilityCooldown> petCooldowns = new List<AbilityCooldown>(3);

    private GameObject player;
    private Abilities abilitySystem;
    private List<float> abilityCooldownTimes;
    private List<float> abilityMaxCooldowns;
    private Character character;

    [Header("Toggle")]
    public Toggle autoToggle;
    private bool isAutoEnabled;

    private List<Pet> activePets = new List<Pet>(3);
    private List<float> petCooldownTimers = new List<float>(3) { 0f, 0f, 0f };

    private void Start()
    {
        StartCoroutine(CheckForPlayer());
        isAutoEnabled = PlayerPrefs.GetString("AutoToggle", "true") == "true";
        InitializePetLists();
    }

    private void InitializePetLists()
    {
        while (activePets.Count < 3)
            activePets.Add(null);
        while (petCooldownTimers.Count < 3)
            petCooldownTimers.Add(0f);
    }

    private void Update()
    {
        UpdatePetCooldowns();
    }

    IEnumerator CheckForPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                abilitySystem = player.GetComponent<Abilities>();
                abilitySystem.cooldownUI = abilityCooldowns;
                abilitySystem.InitializeCooldowns();
                character = player.GetComponent<Character>();
                character.death.AddListener(DeathOfPlayer);
                SetAbilityImages();
                DisableUnusedCooldownUI();
                yield return new WaitForSeconds(0.1f);
                InitializePets();
                if (autoToggle != null)
                {
                    autoToggle.isOn = isAutoEnabled;
                    abilitySystem.ToggleAuto(isAutoEnabled);
                }
                yield break;
            }
        }
        else
        {
            yield break;
        }
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(CheckForPlayer());
    }

    private void InitializePets()
    {
        Pet[] allPets = FindObjectsByType<Pet>(FindObjectsSortMode.None);
        for (int i = 0; i < activePets.Count; i++)
        {
            activePets[i] = null;
            petCooldownTimers[i] = 0f;
        }
        for (int i = 0; i < Mathf.Min(allPets.Length, 3); i++)
        {
            activePets[i] = allPets[i];
            petCooldownTimers[i] = 0f;
        }
        SetupPetUI();
    }

    private void SetupPetUI()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i < activePets.Count && activePets[i] != null)
            {
                Pet currentPet = activePets[i];
                if (i < petImages.Count && petImages[i] != null)
                {
                    if (currentPet.ability != null && currentPet.ability.image != null)
                    {
                        petImages[i].sprite = currentPet.ability.image;
                        petImages[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        petImages[i].gameObject.SetActive(false);
                    }
                }
                if (i < petCooldowns.Count && petCooldowns[i] != null)
                {
                    petCooldowns[i].gameObject.SetActive(true);
                    petCooldowns[i].SetMaxCooldown(currentPet.abilityCooldown);
                }
            }
            else
            {
                if (i < petImages.Count && petImages[i] != null)
                {
                    petImages[i].gameObject.SetActive(false);
                }
                if (i < petCooldowns.Count && petCooldowns[i] != null)
                {
                    petCooldowns[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private IEnumerator TestPetCooldown(int petIndex)
    {
        yield return new WaitForSeconds(1f);
        if (petIndex < activePets.Count && activePets[petIndex] != null)
        {
            Pet testPet = activePets[petIndex];
            petCooldownTimers[petIndex] = testPet.abilityCooldown;
            float originalCooldown = petCooldownTimers[petIndex];
            while (petCooldownTimers[petIndex] > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private void UpdatePetCooldowns()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i >= activePets.Count || activePets[i] == null || i >= petCooldowns.Count || petCooldowns[i] == null)
                continue;
            if (petCooldownTimers[i] > 0)
            {
                petCooldownTimers[i] -= Time.deltaTime;
                petCooldowns[i].SetCooldown(petCooldownTimers[i]);
            }
            else
            {
                petCooldowns[i].SetCooldown(0f);
            }
        }
    }

    public void StartPetCooldown(Pet pet)
    {
        for (int i = 0; i < activePets.Count; i++)
        {
            if (activePets[i] == pet)
            {
                petCooldownTimers[i] = pet.abilityCooldown;
                if (i < petCooldowns.Count && petCooldowns[i] != null)
                {
                    petCooldowns[i].SetMaxCooldown(pet.abilityCooldown);
                    petCooldowns[i].SetCooldown(pet.abilityCooldown);
                }
                return;
            }
        }
    }

    public bool IsPetOnCooldown(Pet pet)
    {
        for (int i = 0; i < activePets.Count; i++)
        {
            if (activePets[i] == pet)
            {
                return petCooldownTimers[i] > 0;
            }
        }
        return false;
    }

    public bool IsPetOnCooldown()
    {
        for (int i = 0; i < petCooldownTimers.Count; i++)
        {
            if (petCooldownTimers[i] > 0)
                return true;
        }
        return false;
    }

    public List<Pet> GetActivePets()
    {
        return activePets;
    }

    public Pet GetActivePet(int index)
    {
        if (index >= 0 && index < activePets.Count)
            return activePets[index];
        return null;
    }

    public void SetPetInSlot(Pet pet, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2)
        {
            return;
        }
        while (activePets.Count <= slotIndex)
        {
            activePets.Add(null);
        }
        while (activePets.Count < 3)
        {
            activePets.Add(null);
        }
        while (petCooldownTimers.Count <= slotIndex)
        {
            petCooldownTimers.Add(0f);
        }
        while (petCooldownTimers.Count < 3)
        {
            petCooldownTimers.Add(0f);
        }
        activePets[slotIndex] = pet;
        petCooldownTimers[slotIndex] = 0f;
        SetupPetUI();
    }

    public void RemovePetFromSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 3)
        {
            if (slotIndex < activePets.Count)
            {
                activePets[slotIndex] = null;
            }
            if (slotIndex < petCooldownTimers.Count)
            {
                petCooldownTimers[slotIndex] = 0f;
            }
            SetupPetUI();
        }
    }

    private void SetAbilityImages()
    {
        if (abilitySystem == null || abilitySystem.abilities == null || images == null)
        {
            Debug.LogWarning("AbilitySystem, abilities list, or images list is null");
            return;
        }

        // Update each ability slot's image
        for (int i = 0; i < images.Count && i < 6; i++)
        {
            if (images[i] != null)
            {
                // Check if there's an ability in this slot
                if (i < abilitySystem.abilities.Count && abilitySystem.abilities[i] != null)
                {
                    Sprite abilitySprite = abilitySystem.abilities[i].image;
                    if (abilitySprite != null)
                    {
                        images[i].sprite = abilitySprite;
                        images[i].color = Color.white; // Make sure it's visible
                        images[i].gameObject.SetActive(true); // Make sure the image is active
                    }
                    else
                    {
                        images[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    // No ability in this slot - hide the image
                    images[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void ToggleAuto(Toggle toggle)
    {
        abilitySystem.ToggleAuto(toggle.isOn);
    }

    public void ManuallyActivateAbilities(int index)
    {
        abilitySystem.ManuallyActivateAbility(index);
    }

    private void DisableUnusedCooldownUI()
    {
        if (abilitySystem == null || abilityCooldowns == null || abilitySystem.abilities == null)
        {
            Debug.LogWarning("Null reference in DisableUnusedCooldownUI");
            return;
        }

        for (int i = 0; i < abilityCooldowns.Count && i < 6; i++)
        {
            if (abilityCooldowns[i] == null) continue;

            bool hasAbility = i < abilitySystem.abilities.Count && abilitySystem.abilities[i] != null;

            // Always keep the cooldown UI active, but show/hide the image
            abilityCooldowns[i].gameObject.SetActive(true);

            if (hasAbility)
            {
                // Set max cooldown for this ability
                abilityCooldowns[i].SetMaxCooldown(abilitySystem.abilities[i].cooldownTime);
            }
        }

        // Update the images after setting up cooldowns
        SetAbilityImages();
    }

    public void UpdateAbilityUI()
    {
        if (abilitySystem != null)
        {
            DisableUnusedCooldownUI(); // This now calls SetAbilityImages() internally
        }
    }

    private void DeathOfPlayer()
    {
        if (character != null)
        {
            character.death.RemoveAllListeners();
        }
        player = null;
        for (int i = 0; i < activePets.Count; i++)
        {
            activePets[i] = null;
            petCooldownTimers[i] = 0f;
        }
        StartCoroutine(CheckForPlayer());
    }
}