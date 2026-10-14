namespace Dairy.Application.DTOs.Auth;

/// <summary>Row returned by sp_GetUserPermissions.</summary>
public class PermissionCodeRow
{
    public string Code { get; set; } = string.Empty;
}
