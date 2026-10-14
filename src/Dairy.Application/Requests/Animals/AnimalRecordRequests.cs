using Dairy.Application.Common;

namespace Dairy.Application.Requests.Animals;

// Property names equal the column names: the generic record repository binds them as SQL parameters.

public class AnimalRecordQuery : PagedQuery
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class AnimalHealthRecordRequest
{
    public DateTime? RecordDate { get; set; }
    public string HealthCondition { get; set; } = string.Empty;
    public string? Symptoms { get; set; }
    public string? Diagnosis { get; set; }
    public string? Treatment { get; set; }
    public string? Medicines { get; set; }
    public string Severity { get; set; } = "Mild";
    public string? VetName { get; set; }
    public long? VetEmployeeId { get; set; }
    public decimal Cost { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime? FollowUpDate { get; set; }
    public DateTime? RecoveredDate { get; set; }
    public string? Remarks { get; set; }
}

public class VaccinationRequest
{
    public string VaccineName { get; set; } = string.Empty;
    public DateTime? VaccinationDate { get; set; }
    public DateTime? NextDueDate { get; set; }
    public int DoseNumber { get; set; }
    public string? BatchNumber { get; set; }
    public string? AdministeredBy { get; set; }
    public long? VetEmployeeId { get; set; }
    public decimal Cost { get; set; }
    public string Status { get; set; } = "Completed";
    public string? Remarks { get; set; }
}

public class DewormingRequest
{
    public DateTime? DewormingDate { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string? AdministeredBy { get; set; }
    public decimal Cost { get; set; }
    public string Status { get; set; } = "Completed";
    public string? Remarks { get; set; }
}

public class BreedingRecordRequest
{
    public DateTime? BreedingDate { get; set; }
    public string BreedingMethod { get; set; } = "ArtificialInsemination";
    public string? BullName { get; set; }
    public string? SemenCode { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? HeatDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Result { get; set; } = "Pending";
    public DateTime? ConfirmedDate { get; set; }
    public decimal Cost { get; set; }
    public string? Remarks { get; set; }
}

public class PregnancyRecordRequest
{
    public long? BreedingRecordId { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string Status { get; set; } = "Pregnant";
    public string? DeliveryOutcome { get; set; }
    public int CalfCount { get; set; }
    public long? CalfAnimalId { get; set; }
    public string? CalfGender { get; set; }
    public DateTime? LastCheckupDate { get; set; }
    public string? Remarks { get; set; }
}

public class AnimalMilkProductionRequest
{
    public DateTime? ProductionDate { get; set; }
    public string ShiftName { get; set; } = "Morning";
    public decimal QuantityLiters { get; set; }
    public decimal? Fat { get; set; }
    public decimal? Snf { get; set; }
    public string? Remarks { get; set; }
}
