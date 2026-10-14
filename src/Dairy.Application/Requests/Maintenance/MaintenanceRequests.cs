using Dairy.Application.Common;

namespace Dairy.Application.Requests.Maintenance;

public class EquipmentQuery : PagedQuery
{
    /// <summary>Operational | UnderMaintenance | Retired</summary>
    public string? Status { get; set; }

    public string? Category { get; set; }
    public long? BranchId { get; set; }
    public long? CollectionCenterId { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateEquipmentRequest
{
    public string EquipmentCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public string? Location { get; set; }
    public long? BranchId { get; set; }
    public long? CollectionCenterId { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? VendorName { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateEquipmentRequest : CreateEquipmentRequest
{
    /// <summary>Null = leave unchanged. Operational | UnderMaintenance | Retired.</summary>
    public string? Status { get; set; }

    /// <summary>Null = leave unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class MaintenanceScheduleQuery : PagedQuery
{
    public long? EquipmentId { get; set; }

    /// <summary>Active | Paused</summary>
    public string? Status { get; set; }

    /// <summary>true = Active schedules whose NextDueDate is within AlertBeforeDays (or overdue).</summary>
    public bool? DueOnly { get; set; }

    public long? AssignedEmployeeId { get; set; }
}

public class CreateMaintenanceScheduleRequest
{
    public long EquipmentId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int FrequencyDays { get; set; } = 30;
    public DateTime? LastServiceDate { get; set; }

    /// <summary>Null = (LastServiceDate ?? today) + FrequencyDays.</summary>
    public DateTime? NextDueDate { get; set; }

    public int AlertBeforeDays { get; set; } = 7;
    public long? AssignedEmployeeId { get; set; }
}

public class UpdateMaintenanceScheduleRequest
{
    public string TaskName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int FrequencyDays { get; set; } = 30;
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextDueDate { get; set; }
    public int AlertBeforeDays { get; set; } = 7;
    public long? AssignedEmployeeId { get; set; }

    /// <summary>Null = leave unchanged. Active | Paused.</summary>
    public string? Status { get; set; }

    /// <summary>Null = leave unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class MaintenanceRecordQuery : PagedQuery
{
    public long? EquipmentId { get; set; }
    public long? ScheduleId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>Preventive | Corrective | Breakdown</summary>
    public string? Type { get; set; }
}

public class CreateMaintenanceRecordRequest
{
    public long EquipmentId { get; set; }

    /// <summary>Set for scheduled (preventive) maintenance; advances the schedule's NextDueDate.</summary>
    public long? MaintenanceScheduleId { get; set; }

    /// <summary>Null = today. Cannot be in the future.</summary>
    public DateTime? MaintenanceDate { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public decimal Cost { get; set; }

    /// <summary>Optional override: Preventive | Corrective | Breakdown (default Preventive when a schedule is given, else Corrective).</summary>
    public string? MaintenanceType { get; set; }

    public string? PerformedBy { get; set; }
    public decimal? DowntimeHours { get; set; }
    public string? InvoiceNo { get; set; }
    public string? Remarks { get; set; }
}
