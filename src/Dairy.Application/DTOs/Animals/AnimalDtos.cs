namespace Dairy.Application.DTOs.Animals;

public class AnimalListDto
{
    public long Id { get; set; }
    public string TagNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public long FarmerId { get; set; }
    public string FarmerCode { get; set; } = string.Empty;
    public string FarmerName { get; set; } = string.Empty;
    public long? BreedId { get; set; }
    public string? BreedName { get; set; }

    /// <summary>Cow | Buffalo | Goat.</summary>
    public string AnimalType { get; set; } = string.Empty;

    /// <summary>Female | Male.</summary>
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? AgeMonths { get; set; }

    /// <summary>Milking | Dry | Heifer | Calf | NotApplicable.</summary>
    public string MilkingStatus { get; set; } = string.Empty;

    /// <summary>NotPregnant | Pregnant | Unknown.</summary>
    public string PregnancyStatus { get; set; } = string.Empty;
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>Active | Sick | Sold | Dead | Archived.</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public bool IsActive { get; set; }
}

public class AnimalDetailDto : AnimalListDto
{
    public string? Color { get; set; }
    public decimal? WeightKg { get; set; }

    /// <summary>Born | Purchased.</summary>
    public string Source { get; set; } = string.Empty;
    public int LactationNo { get; set; }
    public DateTime? LastCalvingDate { get; set; }
    public DateTime? StatusDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }

    /// <summary>Present when the animal was sold.</summary>
    public AnimalSaleDto? Sale { get; set; }

    /// <summary>Present when the animal died.</summary>
    public AnimalDeathDto? Death { get; set; }
}

public class AnimalSaleDto
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public DateTime SaleDate { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerMobile { get; set; }
    public string? BuyerAddress { get; set; }
    public decimal SalePrice { get; set; }
    public long? PaymentModeId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Reason { get; set; }
    public string? Remarks { get; set; }
}

public class AnimalDeathDto
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public DateTime DeathDate { get; set; }
    public string? Cause { get; set; }

    /// <summary>Disease | Accident | Age | Calving | Unknown.</summary>
    public string CauseCategory { get; set; } = string.Empty;
    public string? VetName { get; set; }
    public bool PostMortemDone { get; set; }
    public decimal LossAmount { get; set; }
    public string? Remarks { get; set; }
}

public class BreedDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Cow | Buffalo | Goat.</summary>
    public string AnimalType { get; set; } = string.Empty;
    public decimal? AverageDailyYield { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class AnimalDueDto
{
    public long AnimalId { get; set; }
    public string TagNumber { get; set; } = string.Empty;
    public string? AnimalName { get; set; }
    public long FarmerId { get; set; }
    public string FarmerName { get; set; } = string.Empty;

    /// <summary>Vaccination | Deworming | Pregnancy.</summary>
    public string DueType { get; set; } = string.Empty;

    /// <summary>Vaccine name, medicine name or "Expected delivery".</summary>
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }

    /// <summary>Negative when overdue.</summary>
    public int DaysRemaining { get; set; }

    /// <summary>Status of the underlying record (Completed, Scheduled, Missed, Pregnant).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Id of the underlying vaccination / deworming / pregnancy record.</summary>
    public long RefId { get; set; }
}

public class AnimalLifecycleEventDto
{
    public DateTime EventDate { get; set; }

    /// <summary>Registration | Health | Vaccination | Deworming | Breeding | Pregnancy | Delivery | Sale | Death | Archive.</summary>
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public long? RefId { get; set; }
}
