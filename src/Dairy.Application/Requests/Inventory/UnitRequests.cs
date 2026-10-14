using Dairy.Application.Common;

namespace Dairy.Application.Requests.Inventory;

public class UnitQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

public class CreateUnitRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UpdateUnitRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Null = leave unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}
