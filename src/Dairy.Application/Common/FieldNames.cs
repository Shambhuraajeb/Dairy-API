namespace Dairy.Application.Common;

public static class FieldNames
{
    /// <summary>"Items[0].UnitPrice" -> "items[0].unitPrice"; strips a leading "$." (System.Text.Json paths).</summary>
    public static string ToCamelCase(string? path)
    {
        if (string.IsNullOrEmpty(path) || path == "$") return string.Empty;
        if (path.StartsWith("$.", StringComparison.Ordinal)) path = path.Substring(2);

        var parts = path.Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
                parts[i] = char.ToLowerInvariant(parts[i][0]) + parts[i].Substring(1);
        }
        return string.Join('.', parts);
    }
}
