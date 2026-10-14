using Dairy.Application.Common;

namespace Dairy.Application.Requests.Farmers;

public class FarmerQuery : PagedQuery
{
    /// <summary>Pending | Active | Inactive | Blocked.</summary>
    public string? Status { get; set; }

    /// <summary>Pending | Verified | Rejected.</summary>
    public string? VerificationStatus { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? MilkType { get; set; }

    /// <summary>Set by the service for the Farmer role; ignored from the client.</summary>
    public long? FarmerId { get; set; }
}

/// <summary>Date/status filter for the per-farmer collection and payment tabs.</summary>
public class FarmerSubListQuery : PagedQuery
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Status { get; set; }
}

public class FarmerAddressRequest
{
    /// <summary>Home | Farm | Billing.</summary>
    public string AddressType { get; set; } = "Home";
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? Village { get; set; }
    public string? Taluka { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
}

public class FarmerBankAccountRequest
{
    public string AccountHolderName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string IfscCode { get; set; } = string.Empty;
    public string? UpiId { get; set; }
    public bool IsPrimary { get; set; }
}

public class CreateFarmerRequest
{
    /// <summary>Optional; auto-generated (FRM-000001 ...) when blank.</summary>
    public string? FarmerCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string? AlternateMobile { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AadhaarNo { get; set; }
    public string? PanNo { get; set; }
    public long? CollectionCenterId { get; set; }
    public long? BranchId { get; set; }
    public string MilkType { get; set; } = "Cow";
    public DateTime? JoinDate { get; set; }
    public string? Notes { get; set; }

    public FarmerAddressRequest? Address { get; set; }
    public FarmerBankAccountRequest? BankAccount { get; set; }
}

public class UpdateFarmerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string? AlternateMobile { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AadhaarNo { get; set; }
    public string? PanNo { get; set; }
    public long? CollectionCenterId { get; set; }
    public long? BranchId { get; set; }
    public string MilkType { get; set; } = "Cow";
    public DateTime? JoinDate { get; set; }
    public string? Notes { get; set; }
}

public class VerifyFarmerRequest
{
    /// <summary>true (default) = Verified, false = Rejected.</summary>
    public bool Approved { get; set; } = true;
    public string? Remarks { get; set; }
}

public class FarmerStatusRequest
{
    /// <summary>Required for deactivation.</summary>
    public string? Reason { get; set; }
}

public class FarmerDocumentRequest
{
    public long? DocumentTypeId { get; set; }
    public string? DocumentNumber { get; set; }

    /// <summary>Required when no file is uploaded (metadata-only record).</summary>
    public string? FileName { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateFarmerDocumentRequest
{
    public long? DocumentTypeId { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Remarks { get; set; }

    /// <summary>Pending | Verified | Rejected; null leaves it unchanged.</summary>
    public string? VerificationStatus { get; set; }
}
