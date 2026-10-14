using System.Text.RegularExpressions;

namespace Dairy.Application.BusinessRules;

/// <summary>Allowed values (as stored in the schema column comments) + tiny text helpers for the customers/sales/delivery/billing modules.</summary>
public static class SalesValues
{
    public static readonly string[] CustomerTypes = { "Retail", "Distributor" };
    public static readonly string[] CustomerStatuses = { "Active", "Inactive", "Blocked" };
    public static readonly string[] AddressTypes = { "Home", "Office", "Shop", "Billing", "Delivery" };
    public static readonly string[] Slots = { "Morning", "Evening" };
    public static readonly string[] Frequencies = { "Daily", "Alternate", "Weekly" };
    public static readonly string[] BillingCycles = { "Daily", "Weekly", "Monthly" };
    public static readonly string[] ComplaintCategories = { "Quality", "Delivery", "Billing", "Service", "Other" };
    public static readonly string[] ComplaintPriorities = { "Low", "Medium", "High", "Urgent" };
    public static readonly string[] ComplaintStatuses = { "Open", "Assigned", "InProgress", "Resolved", "Closed" };
    public static readonly string[] VehicleTypes = { "Van", "Truck", "Tanker", "Bike", "Auto" };
    public static readonly string[] VehicleStatuses = { "Available", "OnRoute", "Maintenance", "Inactive" };
    public static readonly string[] ScheduleStatuses = { "Planned", "Dispatched", "Completed" };
    public static readonly string[] PriceCustomerTypes = { "Retail", "Distributor" };

    private static readonly string[] WeekdayNames =
    {
        "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun",
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    };

    private static readonly Regex PincodeRegex = new(@"^[0-9]{6}$", RegexOptions.Compiled);
    private static readonly Regex TimeRegex = new(@"^([01][0-9]|2[0-3]):[0-5][0-9]$", RegexOptions.Compiled);

    /// <summary>Trimmed text or null when blank.</summary>
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsValidPincode(string? value) => string.IsNullOrWhiteSpace(value) || PincodeRegex.IsMatch(value.Trim());

    /// <summary>HH:mm (24h); blank passes.</summary>
    public static bool IsValidTime(string? value) => string.IsNullOrWhiteSpace(value) || TimeRegex.IsMatch(value.Trim());

    /// <summary>Comma list like "Mon,Wed,Fri" (short or full day names); blank passes.</summary>
    public static bool IsValidWeekdayList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 && parts.All(p => WeekdayNames.Contains(p, StringComparer.Ordinal));
    }
}
