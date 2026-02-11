using UnityEngine;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;

public class AbilityFusionManager : MonoBehaviour
{
    private GameObject player;
    private Abilities abilitySystem;
    private List<Ability> recentAbilities = new List<Ability>();
    private List<Coroutine> abilityTimers = new List<Coroutine>();

    [Header("Fusion Settings")]
    public int maxRecentAbilities = 2; // Maximum number of recent abilities to track
    public float fusionWindow = 1f; // Time window for abilities to remain fusable
    public float delay;

    [Header("Visual Integration")]
    public bool enableVisualEffects = true; // Enable SkillIconEffect integration

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Original events
    public delegate void AbilityWindowEvent(Ability ability1, Ability ability2, Ability fusedAbility);
    public event AbilityWindowEvent FusedAbilityWindow;

    public delegate void AbilityFused();
    public event AbilityFused AbilityFusedEvent;

    public delegate void AbilityExpiredEvent(Ability ability);
    public event AbilityExpiredEvent OnAbilityExpired;

    // New events for visual integration
    public delegate void VisualFusionTriggered();
    public event VisualFusionTriggered OnVisualFusionTriggered;

    void Start()
    {
        StartCoroutine(CheckForPlayer());

        // Subscribe to SkillIconEffect events if visual effects are enabled
        if (enableVisualEffects)
        {
            SkillIconEffect.OnFusionAnimationTriggered += OnVisualFusionCompleted;
        }
    }

    void OnDestroy()
    {
        ClearRecentAbilities();
        if (abilitySystem != null)
        {
            abilitySystem.OnAbilityActivated -= RegisterAbility;
        }

        // Unsubscribe from visual events
        if (enableVisualEffects)
        {
            SkillIconEffect.OnFusionAnimationTriggered -= OnVisualFusionCompleted;
        }
    }

    IEnumerator CheckForPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                abilitySystem = player.GetComponent<Abilities>();
                abilitySystem.OnAbilityActivated += RegisterAbility;
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

    public void RegisterAbility(Ability ability)
    {
        // Stop any existing timer for abilities that will be removed
        if (recentAbilities.Count >= maxRecentAbilities)
        {
            RemoveOldestAbility();
        }

        // Add the new ability
        recentAbilities.Add(ability);

        // Start a timer for this ability
        Coroutine timer = StartCoroutine(AbilityWindowTimer(ability));
        abilityTimers.Add(timer);

        if (showDebugLogs)
            Debug.Log($"Registered ability: {ability.name}. Current abilities in window: {recentAbilities.Count}");

        // Check for fusion after adding the new ability
        StartCoroutine(FusionCheck());
    }

    private void RemoveOldestAbility()
    {
        if (recentAbilities.Count > 0)
        {
            Ability oldestAbility = recentAbilities[0];

            // Stop the timer for the oldest ability
            if (abilityTimers.Count > 0)
            {
                if (abilityTimers[0] != null)
                    StopCoroutine(abilityTimers[0]);
                abilityTimers.RemoveAt(0);
            }

            recentAbilities.RemoveAt(0);

            if (showDebugLogs)
                Debug.Log($"Removed oldest ability from fusion window: {oldestAbility.name}");
        }
    }

    IEnumerator AbilityWindowTimer(Ability ability)
    {
        yield return new WaitForSeconds(fusionWindow);

        // Remove the ability from the recent abilities list
        int abilityIndex = recentAbilities.IndexOf(ability);
        if (abilityIndex >= 0)
        {
            recentAbilities.RemoveAt(abilityIndex);

            // Remove the corresponding timer
            if (abilityIndex < abilityTimers.Count)
            {
                abilityTimers.RemoveAt(abilityIndex);
            }

            // Notify that the ability has expired from the fusion window
            OnAbilityExpired?.Invoke(ability);

            if (showDebugLogs)
                Debug.Log($"Ability {ability.name} expired from fusion window after {fusionWindow} seconds");
        }
    }

    IEnumerator FusionCheck()
    {
        if (recentAbilities.Count < 2)
        {
            yield break;
        }

        Ability ability1 = recentAbilities[recentAbilities.Count - 2];
        Ability ability2 = recentAbilities[recentAbilities.Count - 1];

        Ability fusedAbility = CheckFusionCompatibility(ability1, ability2);
        if (fusedAbility != null)
        {
            if (showDebugLogs)
                Debug.Log($"Fusion detected: {ability1.name} + {ability2.name} = {fusedAbility.name}");

            // DELAY the fusion event to allow button clicks to complete
            yield return new WaitForEndOfFrame();

            // Now fire the fusion event (this triggers the visual animation)
            AbilityFusedEvent?.Invoke();

            // Trigger visual fusion event
            if (enableVisualEffects)
            {
                OnVisualFusionTriggered?.Invoke();
            }

            yield return new WaitForSeconds(.5f);
            FusedAbilityWindow?.Invoke(ability1, ability2, fusedAbility);
            yield return new WaitForSeconds(delay);

            // Trigger the fusion result
            abilitySystem.ActivateFusionAbility(fusedAbility);

            if (showDebugLogs)
                Debug.Log($"Applied fusion result: {fusedAbility.name}");

            // Clear the recent abilities since they've been successfully fused
            ClearRecentAbilities();
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"No fusion result for {ability1.name} and {ability2.name}");
        }
    }

    // Visual fusion completion handler
    private void OnVisualFusionCompleted(SkillIconEffect skill1, SkillIconEffect skill2)
    {
        if (showDebugLogs)
            Debug.Log($"Visual fusion animation completed between two skill icons");

        // You can add additional effects here, like:
        // - Play completion sound
        // - Show particle effects
        // - Update UI elements
    }

    // Check if visual fusion is possible
    public bool CanVisuallyFuse()
    {
        if (!enableVisualEffects) return false;

        return SkillIconEffect.GetFusionWindowCount() >= 2;
    }

    // Manually trigger visual fusion (for testing)
    public bool TriggerVisualFusionManually()
    {
        if (!enableVisualEffects) return false;

        return SkillIconEffect.TriggerFusionOnLastTwo();
    }

    // Get info about current visual state
    public int GetVisualFusionCandidateCount()
    {
        return enableVisualEffects ? SkillIconEffect.GetFusionWindowCount() : 0;
    }

    public Sprite GetMergedAbilitySprite(SkillIconEffect skill1, SkillIconEffect skill2)
    {
        // Get the actual Ability objects from your recent abilities list
        if (recentAbilities.Count >= 2)
        {
            Ability ability1 = recentAbilities[recentAbilities.Count - 2];
            Ability ability2 = recentAbilities[recentAbilities.Count - 1];

            // Use your existing fusion logic to get the fused ability
            Ability fusedAbility = CheckFusionCompatibility(ability1, ability2);

            if (fusedAbility != null)
            {
                // Get the sprite from the fused ability's icon
                // Assuming your Ability class has an icon/sprite field
                return fusedAbility.image; // or fusedAbility.sprite, whatever field name you use
            }
        }

        // Fallback: return null to use original icon
        return null;
    }

    private void ClearRecentAbilities()
    {
        // Stop all running timers
        foreach (Coroutine timer in abilityTimers)
        {
            if (timer != null)
                StopCoroutine(timer);
        }

        abilityTimers.Clear();
        recentAbilities.Clear();

        if (showDebugLogs)
        {
            //Debug.Log("Cleared all abilities from fusion window after successful fusion");
        }
    }

    Ability CheckFusionCompatibility(Ability ability1, Ability ability2)
    {
        if (ability1.fusionResult != null && ability2.fusedWith == ability1)
            return ability1.fusionResult;
        if (ability2.fusionResult != null && ability2.fusedWith == ability1)
            return ability2.fusionResult;
        return null;
    }

    // Original public methods
    public List<Ability> GetCurrentAbilitiesInWindow()
    {
        return new List<Ability>(recentAbilities);
    }

    public float GetRemainingTimeForAbility(Ability ability)
    {
        int index = recentAbilities.IndexOf(ability);
        if (index >= 0)
        {
            return fusionWindow * (1f - (index / (float)recentAbilities.Count));
        }
        return 0f;
    }

    public void SetFusionWindow(float newWindow)
    {
        fusionWindow = newWindow;
        if (showDebugLogs)
            Debug.Log($"Fusion window updated to: {fusionWindow} seconds");
    }

    // Visual settings
    public void SetVisualEffectsEnabled(bool enabled)
    {
        if (enableVisualEffects != enabled)
        {
            enableVisualEffects = enabled;

            if (enabled)
            {
                SkillIconEffect.OnFusionAnimationTriggered += OnVisualFusionCompleted;
            }
            else
            {
                SkillIconEffect.OnFusionAnimationTriggered -= OnVisualFusionCompleted;
            }
        }
    }
}