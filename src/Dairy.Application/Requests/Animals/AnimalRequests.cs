using Dairy.Application.Common;

namespace Dairy.Application.Requests.Animals;

public class AnimalQuery : PagedQuery
{
    /// <summary>Forced to the caller's own farmer for the Farmer role.</summary>
    public long? FarmerId { get; set; }
    public long? BreedId { get; set; }

    /// <summary>Active | Sick | Sold | Dead | Archived.</summary>
    public string? Status { get; set; }

    /// <summary>Cow | Buffalo | Goat.</summary>
    public string? AnimalType { get; set; }
    public string? MilkingStatus { get; set; }
    public string? PregnancyStatus { get; set; }
}

public class AnimalDueQuery : PagedQuery
{
    /// <summary>vaccination | deworming | pregnancy.</summary>
    public string? Type { get; set; }

    /// <summary>Include items due within this many days from today (overdue always included). Default 30.</summary>
    public int Days { get; set; } = 30;

    /// <summary>Forced to the caller's own farmer for the Farmer role.</summary>
    public long? FarmerId { get; set; }
}

public class AnimalRequest
{
    public string TagNumber { get; set; } = string.Empty;
    public long FarmerId { get; set; }
    public long? BreedId { get; set; }
    public string? Name { get; set; }
    public string AnimalType { get; set; } = "Cow";
    public string Gender { get; set; } = "Female";
    public DateTime? DateOfBirth { get; set; }
    public string? Color { get; set; }
    public decimal? WeightKg { get; set; }
    public string Source { get; set; } = "Born";
    public DateTime? RegistrationDate { get; set; }
    public int LactationNo { get; set; }
    public DateTime? LastCalvingDate { get; set; }
    public string MilkingStatus { get; set; } = "Milking";
    public string PregnancyStatus { get; set; } = "NotPregnant";
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Remarks { get; set; }
}

public class AnimalSaleRequest
{
    public DateTime? SaleDate { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerMobile { get; set; }
    public string? BuyerAddress { get; set; }
    public decimal SalePrice { get; set; }
    public long? PaymentModeId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Reason { get; set; }
    public string? Remarks { get; set; }
}

public class AnimalDeathRequest
{
    public DateTime? DeathDate { get; set; }
    public string? Cause { get; set; }

    /// <summary>Disease | Accident | Age | Calving | Unknown.</summary>
    public string CauseCategory { get; set; } = "Unknown";
    public string? VetName { get; set; }
    public bool PostMortemDone { get; set; }
    public decimal LossAmount { get; set; }
    public string? Remarks { get; set; }
}

public class AnimalArchiveRequest
{
    public string? Reason { get; set; }
}

public class BreedQuery : PagedQuery
{
    public string? AnimalType { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateBreedRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AnimalType { get; set; } = "Cow";
    public decimal? AverageDailyYield { get; set; }
    public string? Description { get; set; }
}

public class UpdateBreedRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AnimalType { get; set; } = "Cow";
    public decimal? AverageDailyYield { get; set; }
    public string? Description { get; set; }

    /// <summary>Null = unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}
