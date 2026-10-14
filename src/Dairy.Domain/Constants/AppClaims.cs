namespace Dairy.Domain.Constants;

/// <summary>JWT claim names (short names; inbound claim mapping is disabled).</summary>
public static class AppClaims
{
    public const string Subject = "sub";        // username
    public const string Name = "name";          // display name
    public const string Role = "role";
    public const string UserId = "uid";
    public const string FarmerId = "farmerId";
    public const string CustomerId = "customerId";
    public const string EmployeeId = "employeeId";
    public const string Permission = "permission"; // repeated, one per code
}
