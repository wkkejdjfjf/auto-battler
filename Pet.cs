using System.Collections;
using UnityEngine;

public class Pet : MonoBehaviour
{
    public string id;
    public int level;
    public Rarity rarity;
    public Ability ability;
    public float abilityCooldown;
    public PlayerStats player;
    private CooldownHandler cooldownHandler;
    private bool isAbilityActive = false;
    private float currentCooldown = 0f;
    private bool isAutoMode = true;
    private Coroutine abilityCoroutine;

    private void OnEnable()
    {
        Abilities.OnAutoToggleChanged += SetAutoMode;
    }

    private void OnDisable()
    {
        Abilities.OnAutoToggleChanged -= SetAutoMode;
    }

    private void Start()
    {
        cooldownHandler = FindFirstObjectByType<CooldownHandler>();
        player = FindFirstObjectByType<PlayerStats>();
        currentCooldown = 0f;
        abilityCoroutine = StartCoroutine(AbilityEnum());
    }

    private void Update()
    {
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }
    }

    public void SetAutoMode(bool isAuto)
    {
        isAutoMode = isAuto;
    }

    public void ActivateAbility()
    {
        if (cooldownHandler != null && !cooldownHandler.IsPetOnCooldown(this) && !isAbilityActive)
        {
            StartCoroutine(ExecuteAbility());
        }
    }

    private IEnumerator ExecuteAbility()
    {
        isAbilityActive = true;
        if (ability != null && player != null)
        {
            if (ability.typeOfAttack == Type.damage || ability.typeOfAttack == Type.PerEnemy || ability.typeOfAttack == Type.AOE)
            {
                ability.Activate(player.gameObject, 0, player.attack, 0, 0);
            }
            else if (ability.typeOfAttack == Type.buff)
            {
                ability.Activate(player.gameObject);
            }
        }
        if (cooldownHandler != null)
        {
            cooldownHandler.StartPetCooldown(this);
        }
        isAbilityActive = false;
        yield return null;
    }

    IEnumerator AbilityEnum()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            yield return new WaitUntil(() =>
                isAutoMode &&
                currentCooldown <= 0f &&
                (cooldownHandler == null || !cooldownHandler.IsPetOnCooldown(this)) &&
                ability != null &&
                player != null &&
                ability.CanActivate(player.gameObject)
            );

            if (!isAutoMode) continue;

            if (ability.typeOfAttack == Type.damage || ability.typeOfAttack == Type.PerEnemy || ability.typeOfAttack == Type.AOE)
            {
                ability.Activate(player.gameObject, 0, player.attack, 0, 0);
            }
            else if (ability.typeOfAttack == Type.buff)
            {
                ability.Activate(player.gameObject);
            }

            currentCooldown = abilityCooldown;
            if (cooldownHandler != null)
            {
                cooldownHandler.StartPetCooldown(this);
            }
        }
    }
}