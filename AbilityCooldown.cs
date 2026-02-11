using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityCooldown : MonoBehaviour
{
    [SerializeField] private Image imageCd;
    [SerializeField] private TMP_Text textCd;

    private float maxCooldown = 10f; // Default value

    void Start()
    {
        // Hide text at start
        if (textCd != null)
        {
            textCd.gameObject.SetActive(false);
        }

        // Make sure image is set up properly
        if (imageCd != null)
        {
            imageCd.type = Image.Type.Filled;
            imageCd.fillMethod = Image.FillMethod.Radial360;
            imageCd.fillOrigin = (int)Image.Origin360.Top;
            imageCd.fillClockwise = false;
            imageCd.fillAmount = 0f;
        }
    }

    public void SetMaxCooldown(float cooldown)
    {
        maxCooldown = cooldown;
    }

    public void SetCooldown(float remainingCooldown)
    {
        // Handle text display
        if (textCd != null)
        {
            // Show text only during cooldown
            bool inCooldown = remainingCooldown > 0;
            textCd.gameObject.SetActive(inCooldown);

            // Update text
            if (inCooldown)
            {
                textCd.text = Mathf.Round(remainingCooldown) + "s";
            }
        }

        // Handle image fill
        if (imageCd != null)
        {
            if (remainingCooldown > 0)
            {
                // Calculate fill amount - ensures it starts at 1 (full) when cooldown begins
                imageCd.fillAmount = remainingCooldown / maxCooldown;
            }
            else
            {
                // Reset when not in cooldown
                imageCd.fillAmount = 0f;
            }
        }
    }
}