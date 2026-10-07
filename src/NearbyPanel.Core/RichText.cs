namespace NearbyPanel.Core;

/// <summary>
/// Removes Unity rich-text tags (<c>&lt;color=orange&gt;</c>, <c>&lt;/color&gt;</c>, <c>&lt;b&gt;</c>,
/// <c>&lt;size=12&gt;</c>, ...) from a string. The panel draws with rich text off and truncates
/// to a column width, so a tag would otherwise show up as literal text, cut in the middle.
/// Vanilla does this to a few names: the Hildir quest bosses are localized as
/// <c>&lt;color=orange&gt;Zil&lt;/color&gt;</c>.
/// </summary>
public static class RichText
{
    /// <summary>The text without any <c>&lt;...&gt;</c> tag. A lone '&lt;' with no closing '&gt;' is kept.</summary>
    public static string Strip(string value)
    {
        if (string.IsNullOrEmpty(value) || value.IndexOf('<') < 0)
        {
            return value;
        }

        var sb = new System.Text.StringBuilder(value.Length);
        int i = 0;
        while (i < value.Length)
        {
            char c = value[i];
            if (c == '<')
            {
                int close = value.IndexOf('>', i + 1);
                if (close > i)
                {
                    i = close + 1;
                    continue;
                }
            }
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }
}
