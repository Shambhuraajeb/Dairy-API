namespace Dairy.Application.DTOs.Suppliers;

public class SupplierDto
{
    public long Id { get; set; }
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
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int PurchaseCount { get; set; }
    public decimal TotalPurchased { get; set; }
}
