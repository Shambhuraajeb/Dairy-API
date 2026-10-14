namespace Dairy.Application.Common;

/// <summary>Tiny helpers for normalising request strings before they reach the database.</summary>
public static class TextClean
{
    /// <summary>Trimmed text; empty string when null.</summary>
    public static string Req(string? value) => (value ?? string.Empty).Trim();

    /// <summary>Trimmed text; null when null/blank.</summary>
    public static string? Opt(string? value)
    {
        var t = value?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
