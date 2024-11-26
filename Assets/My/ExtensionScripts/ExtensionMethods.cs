using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

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

    public static float ToReadableFloat(this float value)
    {
        if (Mathf.Abs(value) < 1) return Mathf.Round(value * 10f) / 10f;

        if (Mathf.Abs(value) >= 1e12f) return Mathf.RoundToInt(value / 1e12f) * 1e12f;
        if (Mathf.Abs(value) >= 1e9f) return Mathf.RoundToInt(value / 1e9f) * 1e9f;
        if (Mathf.Abs(value) >= 1e6f) return Mathf.RoundToInt(value / 1e6f) * 1e6f;
        if (Mathf.Abs(value) >= 1e3f) return Mathf.RoundToInt(value / 1e3f) * 1e3f;

        return Mathf.RoundToInt(value);
    }

    /// <summary>
    /// Calculates the median value of a list of numeric types.
    /// If an exact median cannot be calculated (e.g., for some generic types),
    /// returns the left middle element as a fallback.
    /// </summary>
    /// <typeparam name="T">A numeric type that supports comparison</typeparam>
    /// <param name="source">The source list of numeric values</param>
    /// <returns>The median value or left middle element of the list</returns>
    /// <exception cref="InvalidOperationException">Thrown when the list is empty</exception>
    public static T Median<T>(this IEnumerable<T> source) where T : IComparable<T>
    {
        // Check if the source is null or empty
        if (source == null || !source.Any())
        {
            throw new InvalidOperationException("Cannot calculate median of an empty list.");
        }

        // Convert to a sorted list
        var sortedList = source.OrderBy(x => x).ToList();
        int count = sortedList.Count;

        // If odd number of elements, return middle element
        if (count % 2 != 0)
        {
            return sortedList[count / 2];
        }

        // For even number of elements, return the left middle element
        // Note: This is a fallback and may not be a true median for all types
        return sortedList[count / 2 - 1];
    }
}
