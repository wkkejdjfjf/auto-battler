using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;

public class TrapeziumReveal : MonoBehaviour
{
    [Header("References")]
    public RectTransform maskContainer;
    public RectTransform ability1Image;
    public RectTransform ability2Image;
    public RectTransform fusedAbilityImage;
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float slideDistance = 300f;
    public float slideDuration = 1f;
    public float delayBeforeClose = 2f;

    private Vector2 originalMaskPos;
    private Vector2 originalAbility1Pos;
    private Vector2 originalAbility2Pos;
    private Vector2 originalFusedAbilityPos;
    private bool isAnimating = false;

    private GameObject player;
    private Abilities abilitySystem;
    private AbilityFusionManager fusionManager;

    public TextMeshProUGUI fusedText;

    public List<ParticleSystem> pSystems;

    private Queue<FusionData> revealQueue = new Queue<FusionData>();

    void Start()
    {
        // Cache positions for all images
        originalMaskPos = maskContainer.anchoredPosition;
        originalAbility1Pos = ability1Image.anchoredPosition;
        originalAbility2Pos = ability2Image.anchoredPosition;
        originalFusedAbilityPos = fusedAbilityImage.anchoredPosition;

        // Start all images off-screen
        maskContainer.anchoredPosition = originalMaskPos + Vector2.left * slideDistance;
        ability1Image.anchoredPosition = originalAbility1Pos + Vector2.left * slideDistance;
        ability2Image.anchoredPosition = originalAbility2Pos + Vector2.left * slideDistance;
        fusedAbilityImage.anchoredPosition = originalFusedAbilityPos + Vector2.left * slideDistance;
        canvasGroup.alpha = 0f;

        fusionManager = FindFirstObjectByType<AbilityFusionManager>();
        if (fusionManager != null)
        {
            fusionManager.FusedAbilityWindow += TriggerReveal;
        }

        //StartCoroutine(CheckForPlayer());
    }

    public void TriggerReveal(Ability ability1, Ability ability2, Ability fusedAbility)
    {
        FusionData fusionData = new FusionData(ability1, ability2, fusedAbility);
        // Add the ability to the queue
        revealQueue.Enqueue(fusionData);

        // If not already showing, start
        if (!isAnimating)
        {
            PlayNextReveal();
        }
    }

    private void PlayNextReveal()
    {
        if (revealQueue.Count == 0)
        {
            isAnimating = false;
            return;
        }

        isAnimating = true;
        FusionData fusionData = revealQueue.Dequeue();

        // Set ability 1 sprite
        Image ability1Img = ability1Image.GetComponent<Image>();
        ability1Img.sprite = fusionData.ability1.image;

        // Set ability 2 sprite
        Image ability2Img = ability2Image.GetComponent<Image>();
        ability2Img.sprite = fusionData.ability2.image;

        // Set fused ability sprite
        Image fusedImg = fusedAbilityImage.GetComponent<Image>();
        fusedImg.sprite = fusionData.fusedAbility.image;

        fusedText.text = fusionData.fusedAbility.name;

        // Reset positions for all images
        maskContainer.anchoredPosition = originalMaskPos + Vector2.left * slideDistance;
        ability1Image.anchoredPosition = originalAbility1Pos + Vector2.left * slideDistance;
        ability2Image.anchoredPosition = originalAbility2Pos + Vector2.left * slideDistance;
        fusedAbilityImage.anchoredPosition = originalFusedAbilityPos + Vector2.left * slideDistance;
        canvasGroup.alpha = 0f;

        Sequence seq = DOTween.Sequence();

        // Fade in & slide in all images together
        seq.Append(canvasGroup.DOFade(1f, 0.3f));
        seq.Join(maskContainer.DOAnchorPos(originalMaskPos, slideDuration).SetEase(Ease.OutExpo));
        seq.Join(ability1Image.DOAnchorPos(originalAbility1Pos, slideDuration).SetEase(Ease.OutExpo));
        seq.Join(ability2Image.DOAnchorPos(originalAbility2Pos, slideDuration).SetEase(Ease.OutExpo));
        seq.Join(fusedAbilityImage.DOAnchorPos(originalFusedAbilityPos, slideDuration).SetEase(Ease.OutExpo));

        foreach (ParticleSystem p in pSystems)
        {
            p.Play();
        }

        // Hold
        seq.AppendInterval(delayBeforeClose);

        // Fade out & slide back all images together
        seq.Append(canvasGroup.DOFade(0f, 0.3f));
        seq.Join(maskContainer.DOAnchorPos(originalMaskPos + Vector2.left * slideDistance, slideDuration).SetEase(Ease.InExpo));
        seq.Join(ability1Image.DOAnchorPos(originalAbility1Pos + Vector2.left * slideDistance, slideDuration).SetEase(Ease.InExpo));
        seq.Join(ability2Image.DOAnchorPos(originalAbility2Pos + Vector2.left * slideDistance, slideDuration).SetEase(Ease.InExpo));
        seq.Join(fusedAbilityImage.DOAnchorPos(originalFusedAbilityPos + Vector2.left * slideDistance, slideDuration).SetEase(Ease.InExpo));

        seq.OnComplete(() =>
        {
            isAnimating = false;
            PlayNextReveal(); // Continue with next ability
        });
    }
}