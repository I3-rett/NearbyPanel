using System;
using System.Collections.Generic;
using System.Linq;

namespace NearbyPanel.Core;

/// <summary>One stack inside a container, as the search sees it.</summary>
/// <param name="Name">The name the game shows, localized and stripped of rich text.</param>
/// <param name="Prefab">The item's prefab name, so a wiki or console id matches too; null when the game has none.</param>
/// <param name="Stack">Items in the stack.</param>
public readonly record struct FoundItem(string Name, string? Prefab, int Stack);

/// <summary>
/// The pure half of <c>nearby_find</c>: which stacks match a query, and how the
/// matches read in the STATUS cell.
/// </summary>
public static class ItemSearch
{
    /// <summary>
    /// Case-insensitive substring on the shown name or the prefab. Blank queries match
    /// nothing: the command treats "no argument" as "remove the pins", never as "list
    /// every chest".
    /// </summary>
    public static bool Matches(FoundItem item, string? query)
    {
        string needle = (query ?? string.Empty).Trim();
        if (needle.Length == 0)
        {
            return false;
        }

        return Contains(item.Name, needle) || Contains(item.Prefab, needle);
    }

    /// <summary>
    /// <c>Asksvin saddle ×1, Lox saddle ×2</c>. Stacks of the same name are summed so a
    /// chest with three part-stacks reads as one line, in first-seen order.
    /// </summary>
    public static string Describe(IEnumerable<FoundItem> items)
    {
        var order = new List<string>();
        var totals = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (FoundItem item in items)
        {
            if (!totals.ContainsKey(item.Name))
            {
                order.Add(item.Name);
                totals[item.Name] = 0;
            }

            totals[item.Name] += item.Stack;
        }

        return string.Join(", ", order.Select(name => name + " ×" + totals[name]));
    }

    private static bool Contains(string? haystack, string needle) =>
        haystack != null && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
}
