using System.Globalization;

namespace Dairy.Application.Common;

/// <summary>Small text/time helpers used by the HR, reports and administration services.</summary>
public static class B5Text
{
    /// <summary>Trimmed value, or null when null/blank.</summary>
    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }

    /// <summary>
    /// Parses a time value for an attendance day. Accepts a full date-time ("2026-10-05T09:30:00") or a time of day ("09:30", "09:30:00")
    /// which is combined with <paramref name="day"/>. Blank input yields null (valid).
    /// </summary>
    public static bool TryParseDayTime(string? value, DateTime day, out DateTime? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        var s = value.Trim();

        var looksLikeTimeOnly = !s.Contains('T') && !s.Contains('-') && !s.Contains('/') && !s.Contains(' ');
        if (looksLikeTimeOnly)
        {
            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts) && ts >= TimeSpan.Zero && ts < TimeSpan.FromDays(1))
            {
                result = day.Date.Add(ts);
                return true;
            }
            return false;
        }

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
        {
            result = dt;
            return true;
        }
        return false;
    }

    /// <summary>"farmerName" -> "Farmer Name".</summary>
    public static string Humanize(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (i == 0) sb.Append(char.ToUpperInvariant(c));
            else if (char.IsUpper(c) && !char.IsUpper(key[i - 1])) sb.Append(' ').Append(c);
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
