using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PartialFlashbangEffect : MonoBehaviour
{
    [Header("Flashbang Settings")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.2f;

    [Header("Overlay Configuration")]
    [Range(0f, 10f)] public float width = 0.5f;
    [Range(0f, 10f)] public float height = 0.5f;
    [Range(-1f, 10f)] public float horizontalPosition = 0f;
    [Range(-1f, 10f)] public float verticalPosition = 0f;

    [Header("Preview")]
    public bool previewInEditor = false;

    private GameObject flashOverlay;

    public void TriggerFlashbang()
    {
        StartCoroutine(FlashbangCoroutine());
    }

    private IEnumerator FlashbangCoroutine()
    {
        // Create overlay if it doesn't exist
        if (flashOverlay == null)
        {
            flashOverlay = new GameObject("FlashbangOverlay");

            // Add Canvas to render BEHIND other UI
            Canvas canvas = flashOverlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // Render behind other UI elements

            // Add Image component
            Image image = flashOverlay.AddComponent<Image>();
            image.color = flashColor;

            // Configure RectTransform
            RectTransform rectTransform = image.rectTransform;

            // Calculate anchor points based on size and position
            Vector2 center = new Vector2(0.5f + horizontalPosition, 0.5f + verticalPosition);
            rectTransform.anchorMin = new Vector2(
                center.x - (width / 2),
                center.y - (height / 2)
            );
            rectTransform.anchorMax = new Vector2(
                center.x + (width / 2),
                center.y + (height / 2)
            );

            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        // Ensure overlay is active
        flashOverlay.SetActive(true);

        // Get CanvasGroup (add if not exists)
        CanvasGroup canvasGroup = flashOverlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = flashOverlay.AddComponent<CanvasGroup>();
        }

        // Fade in and out
        float elapsedTime = 0f;
        while (elapsedTime < flashDuration)
        {
            // Fade in first half, fade out second half
            if (elapsedTime < flashDuration / 2)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / (flashDuration / 2));
            }
            else
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, (elapsedTime - flashDuration / 2) / (flashDuration / 2));
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Disable overlay
        flashOverlay.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!previewInEditor) return;

        // Calculate screen rect
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Calculate overlay dimensions
        Vector2 center = new Vector2(
            screenWidth * (0.5f + horizontalPosition),
            screenHeight * (0.5f + verticalPosition)
        );

        Vector2 size = new Vector2(
            screenWidth * width,
            screenHeight * height
        );

        // Draw preview rect
        Handles.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0.5f);
        Handles.DrawSolidRectangleWithOutline(
            new Rect(
                center.x - (size.x / 2),
                center.y - (size.y / 2),
                size.x,
                size.y
            ),
            flashColor,
            Color.white
        );
    }
#endif
}