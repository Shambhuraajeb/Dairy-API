using System.Text.Json.Serialization;

namespace Dairy.Application.DTOs.Farmers;

public class FarmerListDto
{
    public long Id { get; set; }
    public string FarmerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string MilkType { get; set; } = string.Empty;
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public long? BranchId { get; set; }
    public DateTime JoinDate { get; set; }

    /// <summary>Pending | Verified | Rejected.</summary>
    public string VerificationStatus { get; set; } = string.Empty;

    /// <summary>Pending | Active | Inactive | Blocked.</summary>
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int AnimalCount { get; set; }
}

public class FarmerDetailDto : FarmerListDto
{
    public string? BranchName { get; set; }
    public string? AlternateMobile { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AadhaarNo { get; set; }
    public string? PanNo { get; set; }
    public long? VerifiedBy { get; set; }
    public DateTime? VerifiedDate { get; set; }
    public string? VerificationRemarks { get; set; }
    public string? StatusReason { get; set; }
    public DateTime? ActivatedDate { get; set; }
    public DateTime? DeactivatedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }

    public List<FarmerAddressDto> Addresses { get; set; } = new();
    public List<FarmerBankAccountDto> BankAccounts { get; set; } = new();
    public List<FarmerDocumentDto> Documents { get; set; } = new();
}

public class FarmerAddressDto
{
    public long Id { get; set; }
    public long FarmerId { get; set; }
    public string AddressType { get; set; } = string.Empty;
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

public class FarmerBankAccountDto
{
    public long Id { get; set; }
    public long FarmerId { get; set; }
    public string AccountHolderName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string IfscCode { get; set; } = string.Empty;
    public string? UpiId { get; set; }
    public bool IsPrimary { get; set; }
}

public class FarmerDocumentDto
{
    public long Id { get; set; }
    public long FarmerId { get; set; }
    public long? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public string? DocumentNumber { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>Internal storage path; never serialized.</summary>
    [JsonIgnore]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>False for metadata-only documents (no uploaded file).</summary>
    public bool HasFile { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public DateTime? VerifiedDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>Result of opening a stored farmer document for download.</summary>
public class FarmerDocumentFile
{
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "file";
}

public class FarmerSummaryDto
{
    public long FarmerId { get; set; }
    public int AnimalCount { get; set; }
    public int MilkingAnimalCount { get; set; }
    public int CollectionsThisMonth { get; set; }
    public decimal QuantityThisMonth { get; set; }
    public decimal AmountThisMonth { get; set; }
    public int PendingApprovalCount { get; set; }

    /// <summary>Approved collections not yet settled.</summary>
    public decimal PayableAmount { get; set; }
    public decimal LoanBalance { get; set; }
    public decimal AdvanceBalance { get; set; }
    public DateTime? LastCollectionDate { get; set; }
}

public class FarmerCollectionRowDto
{
    public long Id { get; set; }
    public string CollectionNumber { get; set; } = string.Empty;
    public DateTime CollectionDate { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string CollectionCenterName { get; set; } = string.Empty;
    public decimal QuantityLiters { get; set; }
    public decimal Fat { get; set; }
    public decimal Snf { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SettlementStatus { get; set; } = string.Empty;
}

public class FarmerPaymentRowDto
{
    public long Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetPayable { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
}
