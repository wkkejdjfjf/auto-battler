using System;
using UnityEngine;

/// <summary>
/// Formats large numbers into readable short-hand notation (K, M, B, T, etc.)
/// Keeps numbers clean and prevents UI overflow in idle/incremental games
/// </summary>
public static class BigNumberFormatter
{
    // Standard idle game suffixes - covers numbers up to 10^66
    private static readonly string[] suffixes = new string[]
    {
        "",     // 10^0  (1)
        "K",    // 10^3  (1,000)
        "M",    // 10^6  (1,000,000)
        "B",    // 10^9  (1,000,000,000)
        "T",    // 10^12 (1 trillion)
        "Qa",   // 10^15 (1 quadrillion)
        "Qi",   // 10^18 (1 quintillion)
        "Sx",   // 10^21 (1 sextillion)
        "Sp",   // 10^24 (1 septillion)
        "Oc",   // 10^27 (1 octillion)
        "No",   // 10^30 (1 nonillion)
        "Dc",   // 10^33 (1 decillion)
        "Ud",   // 10^36 (1 undecillion)
        "Dd",   // 10^39 (1 duodecillion)
        "Td",   // 10^42 (1 tredecillion)
        "Qad",  // 10^45 (1 quattuordecillion)
        "Qid",  // 10^48 (1 quindecillion)
        "Sxd",  // 10^51 (1 sexdecillion)
        "Spd",  // 10^54 (1 septendecillion)
        "Ocd",  // 10^57 (1 octodecillion)
        "Nod",  // 10^60 (1 novemdecillion)
        "Vg"    // 10^63 (1 vigintillion)
    };

    /// <summary>
    /// Formats a number into short notation
    /// </summary>
    /// <param name="number">The number to format</param>
    /// <param name="decimals">Number of decimal places (default: 2)</param>
    /// <returns>Formatted string (e.g., "1.23M", "456.78K")</returns>
    public static string Format(float number, int decimals = 2)
    {
        // Handle negative numbers
        if (number < 0)
            return "-" + Format(-number, decimals);

        // Handle special cases
        if (float.IsNaN(number))
            return "NaN";
        if (float.IsInfinity(number))
            return "∞";

        // Numbers under 1000 show with no suffix
        if (number < 1000f)
            return number.ToString("F0"); // No decimals for small numbers

        // Find appropriate suffix
        int suffixIndex = 0;
        double workingNumber = number;

        while (workingNumber >= 1000.0 && suffixIndex < suffixes.Length - 1)
        {
            workingNumber /= 1000.0;
            suffixIndex++;
        }

        // Format with appropriate decimals
        string format = "F" + decimals;
        return workingNumber.ToString(format) + suffixes[suffixIndex];
    }

    /// <summary>
    /// Formats a double into short notation (for very large numbers)
    /// </summary>
    public static string Format(double number, int decimals = 2)
    {
        if (number < 0)
            return "-" + Format(-number, decimals);

        if (double.IsNaN(number))
            return "NaN";
        if (double.IsInfinity(number))
            return "∞";

        if (number < 1000.0)
            return number.ToString("F0");

        int suffixIndex = 0;
        double workingNumber = number;

        while (workingNumber >= 1000.0 && suffixIndex < suffixes.Length - 1)
        {
            workingNumber /= 1000.0;
            suffixIndex++;
        }

        string format = "F" + decimals;
        return workingNumber.ToString(format) + suffixes[suffixIndex];
    }

    /// <summary>
    /// Formats an integer into short notation
    /// </summary>
    public static string Format(int number, int decimals = 2)
    {
        return Format((float)number, decimals);
    }

    /// <summary>
    /// Formats a long into short notation
    /// </summary>
    public static string Format(long number, int decimals = 2)
    {
        return Format((double)number, decimals);
    }

    /// <summary>
    /// Gets the suffix for a given number without formatting
    /// Useful for achievement tracking ("Reach 1M gold")
    /// </summary>
    public static string GetSuffix(float number)
    {
        if (number < 1000f)
            return "";

        int suffixIndex = 0;
        double workingNumber = number;

        while (workingNumber >= 1000.0 && suffixIndex < suffixes.Length - 1)
        {
            workingNumber /= 1000.0;
            suffixIndex++;
        }

        return suffixes[suffixIndex];
    }

    /// <summary>
    /// Checks if a number has reached a new suffix tier
    /// Useful for triggering achievements/events
    /// </summary>
    public static bool HasReachedNewTier(float previousNumber, float currentNumber)
    {
        string previousSuffix = GetSuffix(previousNumber);
        string currentSuffix = GetSuffix(currentNumber);

        return previousSuffix != currentSuffix;
    }
}

// ========================
// USAGE EXAMPLES:
// ========================
/*

// In your UI scripts:
goldText.text = BigNumberFormatter.Format(playerGold);
// Output: "1.23K" or "456.78M"

// In damage display:
healthText.text = BigNumberFormatter.Format(currentHealth);
// Output: "999" or "12.5K"

// For whole numbers (no decimals):
waveText.text = BigNumberFormatter.Format(currentWave, 0);
// Output: "50" or "1K"

// Achievement detection:
if (BigNumberFormatter.HasReachedNewTier(oldGold, newGold))
{
    Debug.Log("Achievement unlocked: Reached " + BigNumberFormatter.GetSuffix(newGold) + " gold!");
}

// In CharacterStatsBase.cs UpdateHealthUI():
protected virtual void UpdateHealthUI()
{
    if (healthSlider != null)
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }
    if (healthText != null)
    {
        healthText.text = BigNumberFormatter.Format(currentHealth, 0); // No decimals for HP
    }
}

// In damage calculation display:
DynamicTextManager.CreateText2D(
    transform.position, 
    BigNumberFormatter.Format(damageTaken, 1), // 1 decimal for damage
    textData
);

*/