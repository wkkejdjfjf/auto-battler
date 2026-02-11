using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class AbilityDragDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Settings")]
    public Canvas canvas;
    public float returnSpeed = 10f;
    public float scaleOnDrag = 1.2f;
    public float holdTimeRequired = 0.3f;

    [Header("Ability Data")]
    public string abilityName;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public AudioClip returnSound;

    [Header("Drag State")]
    public bool isDragging = false;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Coroutine returnRoutine;
    private AbilitySlot currentSlot;
    private bool isPointerDown = false;
    private bool canDrag = false;
    private Coroutine holdTimerRoutine;

    public AbilitySlot CurrentSlot => currentSlot;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        originalScale = transform.localScale;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        canDrag = false;
        holdTimerRoutine = StartCoroutine(HoldTimer());
        PlaySound(pickupSound);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        if (holdTimerRoutine != null)
        {
            StopCoroutine(holdTimerRoutine);
        }
        if (!canDrag)
        {
            StartCoroutine(ScaleDown());
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag)
        {
            return;
        }

        isDragging = true; // Add this line

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
        }
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }
        }
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canDrag && canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isPointerDown = false;
        isDragging = false;

        if (holdTimerRoutine != null)
        {
            StopCoroutine(holdTimerRoutine);
        }
        if (!canDrag)
        {
            return;
        }
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        bool droppedOnSlot = false;
        if (eventData.pointerEnter != null)
        {
            AbilitySlot slot = eventData.pointerEnter.GetComponent<AbilitySlot>();
            if (slot != null)
            {
                droppedOnSlot = true;
                PlaySound(dropSound);
                transform.localScale = originalScale;
                canDrag = false;
            }
        }
        if (!droppedOnSlot)
        {
            ReturnToInventory();
        }
    }

    private IEnumerator HoldTimer()
    {
        yield return new WaitForSeconds(holdTimeRequired);
        if (isPointerDown)
        {
            canDrag = true;
            StartCoroutine(PopScale());
        }
    }

    private IEnumerator PopScale()
    {
        Vector3 startScale = originalScale;
        Vector3 targetScale = originalScale * scaleOnDrag;
        float timer = 0f;
        float popTime = 0.1f;
        while (timer < popTime)
        {
            timer += Time.deltaTime;
            float progress = timer / popTime;
            float curved = Mathf.Sin(progress * Mathf.PI * 0.5f);
            transform.localScale = Vector3.Lerp(startScale, targetScale, curved);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    public AbilitySlot GetCurrentSlot()
    {
        return currentSlot;
    }

    private IEnumerator ScaleDown()
    {
        Vector3 currentScale = transform.localScale;
        float timer = 0f;
        float scaleDownTime = 0.2f;
        while (timer < scaleDownTime)
        {
            timer += Time.deltaTime;
            float progress = timer / scaleDownTime;
            transform.localScale = Vector3.Lerp(currentScale, originalScale, progress);
            yield return null;
        }
        transform.localScale = originalScale;
        canDrag = false;
    }

    private void ReturnToInventory()
    {
        if (currentSlot != null)
        { 
            AbilityManager abilityManager = FindFirstObjectByType<AbilityManager>();
            if (abilityManager != null)
            {
                abilityManager.UnequipAbility(currentSlot.index);
                Destroy(gameObject);
                return;
            }
        }
        AbilityManager abilityManager2 = FindFirstObjectByType<AbilityManager>();
        if (abilityManager2 != null)
        {
            if (abilityManager2.unlockedContainer == null)
            {
                abilityManager2.CreateAbilitiesList();
            }
            Transform targetContainer = abilityManager2.unlockedContainer != null ?
                abilityManager2.unlockedContainer : abilityManager2.invContainer;
            returnRoutine = StartCoroutine(ReturnToPosition(targetContainer, Vector3.zero));
            PlaySound(returnSound);
        }
    }

    private IEnumerator ReturnToPosition(Transform targetParent, Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetWorldPosition = targetParent.TransformPoint(targetPosition);
        float journey = 0f;
        float journeyTime = 1f / returnSpeed;
        while (journey <= journeyTime)
        {
            journey += Time.deltaTime;
            float fractionOfJourney = journey / journeyTime;
            fractionOfJourney = Mathf.SmoothStep(0, 1, fractionOfJourney);
            transform.position = Vector3.Lerp(startPosition, targetWorldPosition, fractionOfJourney);
            yield return null;
        }
        transform.SetParent(targetParent);
        transform.localPosition = targetPosition;
        transform.localScale = originalScale;
        SetCurrentSlot(null);
        canDrag = false;
    }

    public void SetCurrentSlot(AbilitySlot slot)
    {
        currentSlot = slot;
    }

    public void SetAbilityName(string name)
    {
        abilityName = name;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private float lastClickTime = 0;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - lastClickTime < 0.3f)
        {
            OnDoubleClick();
        }
        lastClickTime = Time.time;
    }

    public void OnDoubleClick()
    {
        if (currentSlot != null)
        {
            ReturnToInventory();
        }
    }
}