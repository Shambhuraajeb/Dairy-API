namespace Dairy.Domain.Constants;

/// <summary>Role names exactly as seeded in the Role table (ids 1..13 in this order).</summary>
public static class Roles
{
    public const string SuperAdmin = "Super Admin";
    public const string DairyOwner = "Dairy Owner";
    public const string DairyManager = "Dairy Manager";
    public const string CollectionSupervisor = "Collection Supervisor";
    public const string Accountant = "Accountant";
    public const string InventoryManager = "Inventory Manager";
    public const string DeliveryManager = "Delivery Manager";
    public const string Driver = "Driver / Delivery Agent";
    public const string Veterinarian = "Veterinarian / Animal Staff";
    public const string Employee = "Employee / Staff";
    public const string Farmer = "Farmer";
    public const string Customer = "Customer";
    public const string Distributor = "Distributor / B2B Customer";

    /// <summary>Roles whose data is restricted to ICurrentUser.CustomerId.</summary>
    public static readonly IReadOnlyList<string> CustomerRoles = new[] { Customer, Distributor };

    public static bool IsCustomerRole(string? role) =>
        role != null && CustomerRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
