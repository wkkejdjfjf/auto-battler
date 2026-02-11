using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System;
using System.Collections;

public class SkillIconEffect : MonoBehaviour
{
    [Header("Animation Settings")]
    public float moveUpDistance = 15f;
    public float moveUpDuration = 0.65f;
    public float pauseDuration = 0.2f;
    public float fadeDuration = 0.75f;
    public Ease moveUpEase = Ease.OutQuad;

    [Header("Visual Settings")]
    [Range(0.1f, 1f)]
    public float initialAlpha = 0.7f;

    [Header("References")]
    public Image abilityIcon;
    public Button button;

    private GameObject currentAnimIcon;
    private bool isInFusionWindow = false;
    private bool isAnimating = false;
    private float animationStartTime;
    private Sequence currentSequence;
    private bool isCleaningUp = false;

    private static List<SkillIconEffect> activeAnimations = new List<SkillIconEffect>();
    private static AbilityFusionManager fusionManager;

    public static event Action<SkillIconEffect> OnAnimationStarted;
    public static event Action<SkillIconEffect> OnAnimationEnded;
    public static event Action<SkillIconEffect, SkillIconEffect> OnFusionAnimationTriggered;

    private void Start()
    {
        InitializeComponents();
        FindFusionManager();
        StartCoroutine(SubscribeToAbilitySystem());
    }

    private void OnDestroy()
    {
        ForceCleanupAnimation();
    }

    #region Initialization
    private void InitializeComponents()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (abilityIcon == null)
            abilityIcon = GetComponentInChildren<Image>();
    }

    private static void FindFusionManager()
    {
        if (fusionManager == null)
        {
            fusionManager = FindFirstObjectByType<AbilityFusionManager>();
            if (fusionManager != null)
            {
                fusionManager.AbilityFusedEvent += OnFusionDetected;
            }
        }
    }

    private IEnumerator SubscribeToAbilitySystem()
    {
        GameObject player = GameObject.FindWithTag("Player");
        while (player == null)
        {
            yield return new WaitForSeconds(0.1f);
            player = GameObject.FindWithTag("Player");
        }

        Abilities abilities = player.GetComponent<Abilities>();
        if (abilities != null)
        {
            abilities.OnAbilityActivated += OnAbilityWasActivated;
        }
    }
    #endregion

    #region Animation Logic
    private void OnAbilityWasActivated(Ability activatedAbility)
    {
        if (DoesThisButtonMatchAbility(activatedAbility))
        {
            StartMoveUpAnimation();
        }
    }

    private bool DoesThisButtonMatchAbility(Ability activatedAbility)
    {
        return abilityIcon != null && abilityIcon.sprite == activatedAbility.image;
    }

    private void StartMoveUpAnimation()
    {
        if (isAnimating || currentAnimIcon != null || isCleaningUp) return;
        if (Time.time < 3f) return;
        if (IsAbilityAlreadyAnimating()) return;
        if (HasRecentAnimation()) return;

        isAnimating = true;
        isInFusionWindow = true;
        animationStartTime = Time.time;

        AddToActiveAnimations();

        GameObject animIcon = CreateAnimationIcon();
        if (animIcon != null)
        {
            currentAnimIcon = animIcon;
            AnimateIcon(animIcon);
        }
        else
        {
            ResetAnimationState();
        }
    }

    private bool HasRecentAnimation()
    {
        return Time.time - animationStartTime < 0.5f;
    }

    private bool IsAbilityAlreadyAnimating()
    {
        if (abilityIcon == null || abilityIcon.sprite == null) return false;

        foreach (var activeSkill in activeAnimations)
        {
            if (activeSkill != null && activeSkill != this &&
                activeSkill.abilityIcon != null &&
                activeSkill.abilityIcon.sprite == this.abilityIcon.sprite &&
                activeSkill.isAnimating)
            {
                return true;
            }
        }
        return false;
    }

    private GameObject CreateAnimationIcon()
    {
        if (abilityIcon == null || abilityIcon.sprite == null) return null;

        Transform originalParent = abilityIcon.transform.parent;
        if (originalParent == null) return null;

        string cloneName = $"AnimClone_{abilityIcon.sprite.name}_{Time.frameCount}";
        GameObject animClone = new GameObject(cloneName);
        animClone.transform.SetParent(originalParent, false);

        RectTransform originalRT = abilityIcon.GetComponent<RectTransform>();
        RectTransform cloneRT = animClone.AddComponent<RectTransform>();
        cloneRT.anchorMin = originalRT.anchorMin;
        cloneRT.anchorMax = originalRT.anchorMax;
        cloneRT.pivot = originalRT.pivot;
        cloneRT.sizeDelta = originalRT.sizeDelta;
        cloneRT.anchoredPosition = originalRT.anchoredPosition;
        cloneRT.localRotation = originalRT.localRotation;
        cloneRT.localScale = originalRT.localScale;

        Image cloneImage = animClone.AddComponent<Image>();
        cloneImage.sprite = abilityIcon.sprite;
        cloneImage.color = abilityIcon.color;
        cloneImage.material = abilityIcon.material;
        cloneImage.raycastTarget = false;

        CanvasGroup cg = animClone.AddComponent<CanvasGroup>();
        cg.alpha = initialAlpha;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        animClone.transform.SetAsLastSibling();

        return animClone;
    }

    private void AnimateIcon(GameObject animIcon)
    {
        if (animIcon == null) return;

        RectTransform rt = animIcon.GetComponent<RectTransform>();
        CanvasGroup cg = animIcon.GetComponent<CanvasGroup>();

        if (rt == null || cg == null)
        {
            ForceCleanupAnimation();
            return;
        }

        Vector2 startPos = rt.anchoredPosition;
        Vector2 targetPos = new Vector2(startPos.x, startPos.y + moveUpDistance);

        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
            currentSequence = null;
        }

        currentSequence = DOTween.Sequence();
        currentSequence.AppendInterval(0.15f);
        currentSequence.Append(rt.DOAnchorPos(targetPos, 0.6f).SetEase(Ease.OutCubic));
        currentSequence.Join(cg.DOFade(0f, 0.6f).SetEase(Ease.OutQuad));
        currentSequence.AppendCallback(() => { OnAnimationStarted?.Invoke(this); });
        currentSequence.AppendInterval(pauseDuration);
        currentSequence.AppendCallback(() => { if (isInFusionWindow) EndFusionWindow(); });
        currentSequence.OnComplete(() => { CleanupAnimation(); });
        currentSequence.OnKill(() => { CleanupAnimation(); });

        StartCoroutine(ForceCleanupAfterDelay(3f));
    }

    private IEnumerator ForceCleanupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentAnimIcon != null && !isCleaningUp)
        {
            ForceCleanupAnimation();
        }
    }

    private void EndFusionWindow()
    {
        isInFusionWindow = false;
        OnAnimationEnded?.Invoke(this);
    }
    #endregion

    #region State Management
    private void AddToActiveAnimations()
    {
        if (!activeAnimations.Contains(this)) activeAnimations.Add(this);
        while (activeAnimations.Count > 6)
        {
            var oldest = activeAnimations[0];
            activeAnimations.RemoveAt(0);
            if (oldest != null && oldest != this) oldest.ForceCleanupAnimation();
        }
        activeAnimations.RemoveAll(skill => skill == null);
    }

    private void ResetAnimationState()
    {
        isAnimating = false;
        isInFusionWindow = false;
        currentAnimIcon = null;
        currentSequence = null;
        isCleaningUp = false;
        activeAnimations.Remove(this);
    }

    public void ForceCleanupAnimation()
    {
        if (isCleaningUp) return;
        isCleaningUp = true;

        if (currentSequence != null)
        {
            if (currentSequence.IsActive()) currentSequence.Kill();
            currentSequence = null;
        }

        if (currentAnimIcon != null)
        {
            DOTween.Kill(currentAnimIcon, true);
            Destroy(currentAnimIcon);
            currentAnimIcon = null;
        }

        ResetAnimationState();
    }

    private void CleanupAnimation()
    {
        if (isCleaningUp) return;
        isCleaningUp = true;

        if (currentSequence != null)
        {
            if (currentSequence.IsActive()) currentSequence.Kill();
            currentSequence = null;
        }

        if (currentAnimIcon != null)
        {
            DOTween.Kill(currentAnimIcon, true);
            Destroy(currentAnimIcon);
            currentAnimIcon = null;
        }

        ResetAnimationState();
    }
    #endregion

    #region Fusion System
    private static void OnFusionDetected()
    {
        List<SkillIconEffect> fusionCandidates = GetSkillsInFusionWindow();

        if (fusionCandidates.Count >= 2)
        {
            SkillIconEffect skill1 = fusionCandidates[fusionCandidates.Count - 2];
            SkillIconEffect skill2 = fusionCandidates[fusionCandidates.Count - 1];

            skill1.isInFusionWindow = false;
            skill2.isInFusionWindow = false;

            OnFusionAnimationTriggered?.Invoke(skill1, skill2);
        }
    }

    public static List<SkillIconEffect> GetSkillsInFusionWindow()
    {
        activeAnimations.RemoveAll(skill => skill == null);
        List<SkillIconEffect> inFusionWindow = new List<SkillIconEffect>();
        foreach (SkillIconEffect skill in activeAnimations)
        {
            if (skill != null && skill.isInFusionWindow) inFusionWindow.Add(skill);
        }
        return inFusionWindow;
    }

    public static bool TriggerFusionOnLastTwo()
    {
        List<SkillIconEffect> candidates = GetSkillsInFusionWindow();
        if (candidates.Count >= 2)
        {
            SkillIconEffect skill1 = candidates[candidates.Count - 2];
            SkillIconEffect skill2 = candidates[candidates.Count - 1];

            skill1.isInFusionWindow = false;
            skill2.isInFusionWindow = false;

            OnFusionAnimationTriggered?.Invoke(skill1, skill2);
            return true;
        }
        return false;
    }
    #endregion

    #region Static Utilities
    public static void ForceCleanupAllAnimations()
    {
        var animationsToClean = new List<SkillIconEffect>(activeAnimations);
        foreach (var skill in animationsToClean)
        {
            if (skill != null) skill.ForceCleanupAnimation();
        }
        activeAnimations.Clear();
        DOTween.KillAll();
    }

    public static List<SkillIconEffect> GetActiveAnimations()
    {
        activeAnimations.RemoveAll(skill => skill == null);
        return new List<SkillIconEffect>(activeAnimations);
    }

    public static int GetFusionWindowCount()
    {
        int count = 0;
        foreach (SkillIconEffect skill in activeAnimations)
        {
            if (skill != null && skill.isInFusionWindow) count++;
        }
        return count;
    }

    public static int GetActiveCloneCount()
    {
        int count = 0;
        foreach (SkillIconEffect skill in activeAnimations)
        {
            if (skill != null && skill.currentAnimIcon != null) count++;
        }
        return count;
    }
    #endregion

    #region Properties
    public bool IsInFusionWindow => isInFusionWindow;
    public bool IsAnimating => isAnimating;
    public GameObject CurrentAnimIcon => currentAnimIcon;
    public float TimeSinceAnimationStart => Time.time - animationStartTime;
    #endregion
}
