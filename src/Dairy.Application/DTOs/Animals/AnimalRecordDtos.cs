namespace Dairy.Application.DTOs.Animals;

// Property names equal the column names so the generic record repository can SELECT them directly.

public class AnimalHealthRecordDto
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public DateTime RecordDate { get; set; }
    public string HealthCondition { get; set; } = string.Empty;
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public string? Medicines { get; set; }

    /// <summary>Mild | Moderate | Severe.</summary>
    public string Severity { get; set; } = string.Empty;
    public string? VetName { get; set; }
    public long? VetEmployeeId { get; set; }
    public decimal Cost { get; set; }

    /// <summary>Open | Ongoing | Recovered.</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime? FollowUpDate { get; set; }
    public DateTime? RecoveredDate { get; set; }
    public string? Remarks { get; set; }
}

public class VaccinationDto
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateTime VaccinationDate { get; set; }
    public DateTime? NextDueDate { get; set; }
    public int DoseNumber { get; set; }
    public string? BatchNumber { get; set; }
    public string? AdministeredBy { get; set; }
    public long? VetEmployeeId { get; set; }
    public decimal Cost { get; set; }

    /// <summary>Completed | Scheduled | Missed.</summary>
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class DewormingDto
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public DateTime DewormingDate { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string? AdministeredBy { get; set; }
    public decimal Cost { get; set; }

    /// <summary>Completed | Scheduled | Missed.</summary>
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class BreedingRecordDto
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public DateTime BreedingDate { get; set; }

    /// <summary>NaturalService | ArtificialInsemination.</summary>
    public string BreedingMethod { get; set; } = string.Empty;
    public string? BullName { get; set; }
    public string? SemenCode { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? HeatDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>Pending | Successful | Failed.</summary>
    public string Result { get; set; } = string.Empty;
    public DateTime? ConfirmedDate { get; set; }
    public decimal Cost { get; set; }
    public string? Remarks { get; set; }
}

public class PregnancyRecordDto
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public long? BreedingRecordId { get; set; }
    public DateTime ConfirmedDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>Pregnant | Delivered | Aborted.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>LiveBirth | Stillbirth | Abortion.</summary>
    public string? DeliveryOutcome { get; set; }
    public int CalfCount { get; set; }
    public long? CalfAnimalId { get; set; }
    public string? CalfGender { get; set; }
    public DateTime? LastCheckupDate { get; set; }
    public string? Remarks { get; set; }
}

public class AnimalMilkProductionDto
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public DateTime ProductionDate { get; set; }

    /// <summary>Morning | Evening.</summary>
    public string ShiftName { get; set; } = string.Empty;
    public decimal QuantityLiters { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Snf { get; set; }
    public string? Remarks { get; set; }
}
