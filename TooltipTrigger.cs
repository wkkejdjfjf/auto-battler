using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Content to display in the tooltip
    [TextArea(3, 10)]
    public string tooltipContent;

    // Optional header for the tooltip
    public string tooltipHeader;

    // Reference to the tooltip prefab/instance
    private static GameObject tooltipInstance;

    // Reference to the tooltip text components
    private static TMP_Text headerText;
    private static TMP_Text contentText;

    // Tooltip positioning offset
    public Vector2 offset = new Vector2(20, 20);

    // Time delay before showing tooltip (in seconds)
    public float showDelay = 0.5f;

    // Private fields for managing show/hide logic
    private bool isPointerOver = false;
    private float showTimer = 0f;

    private Vector2 tooltipPosition; // Store position for tooltip after mouse enters

    void Start()
    {
        // Ensure we have a tooltip instance reference
        if (tooltipInstance == null)
        {
            tooltipInstance = TooltipInstance.instance.gameObject;

            if (tooltipInstance != null)
            {
                // Get references to text components
                headerText = tooltipInstance.transform.Find("Header").GetComponent<TMP_Text>();
                contentText = tooltipInstance.transform.Find("Content").GetComponent<TMP_Text>();

                // Hide tooltip initially
                tooltipInstance.SetActive(false);
            }
            else
            {
                Debug.LogWarning("No tooltip object found in scene. Add a tooltip prefab with 'Tooltip' tag.");
            }
        }
    }

    void Update()
    {
        // Handle tooltip show delay
        if (isPointerOver)
        {
            showTimer += Time.deltaTime;
            if (showTimer >= showDelay && !tooltipInstance.activeSelf)
            {
                ShowTooltip();
            }
        }

        // Tooltip positioning logic when the tooltip is active
        if (tooltipInstance != null && tooltipInstance.activeSelf)
        {
            // Tooltip should not follow the mouse after initial position, so stop moving it.
            RectTransform tooltipRect = tooltipInstance.GetComponent<RectTransform>();

            // If the tooltip has been positioned once, stop moving it
            if (tooltipPosition != Vector2.zero)
            {
                tooltipRect.localPosition = tooltipPosition; // Set to initial position where tooltip was spawned
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        showTimer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        showTimer = 0f;
        HideTooltip();
    }

    void OnDisable()
    {
        // Hide tooltip when object is disabled
        if (isPointerOver)
        {
            HideTooltip();
            isPointerOver = false;
        }
    }

    private void ShowTooltip()
    {
        if (tooltipInstance != null)
        {
            // Set tooltip content
            headerText.text = tooltipHeader;
            contentText.text = tooltipContent;

            // Adjust tooltip size based on content if needed
            RectTransform rt = tooltipInstance.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            // Convert the position to screen space and add the offset for the tooltip to appear next to the pointer
            Canvas parentCanvas = tooltipInstance.GetComponentInParent<Canvas>();
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                cam,
                out localPoint
            );

            // Add offset to the local position
            tooltipPosition = localPoint + offset;

            // Set the tooltip's position relative to the canvas
            tooltipInstance.transform.localPosition = tooltipPosition;

            // Show the tooltip
            tooltipInstance.SetActive(true);
        }
    }

    private void HideTooltip()
    {
        if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(false);
        }
    }
}
