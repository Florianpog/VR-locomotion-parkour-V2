using UnityEngine;

public static class ExtensionMethods
{
    /// <summary>
    /// Converts a float to a readable string with grouped '000' notation.
    /// </summary>
    public static string ToReadableFormat(this float value)
    {
        if (Mathf.Abs(value) < 1) return "0"; // Ignore near-zero values

        if (Mathf.Abs(value) >= 1e12f) return Mathf.RoundToInt(value / 1e12f) + "'000'000'000'000";
        if (Mathf.Abs(value) >= 1e9f) return Mathf.RoundToInt(value / 1e9f) + "'000'000'000";
        if (Mathf.Abs(value) >= 1e6f) return Mathf.RoundToInt(value / 1e6f) + "'000'000";
        if (Mathf.Abs(value) >= 1e3f) return Mathf.RoundToInt(value / 1e3f) + "'000";

        return Mathf.RoundToInt(value).ToString();
    }
}
