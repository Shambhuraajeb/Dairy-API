using Dairy.Application.Common;

namespace Dairy.Application.Requests.Suppliers;

public class SupplierQuery : PagedQuery
{
    public bool? IsActive { get; set; }
}

public class CreateSupplierRequest
{
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? GstNumber { get; set; }
    public string? PanNo { get; set; }
    public int PaymentTermsDays { get; set; }
}

public class UpdateSupplierRequest : CreateSupplierRequest
{
    /// <summary>Null = leave unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}
