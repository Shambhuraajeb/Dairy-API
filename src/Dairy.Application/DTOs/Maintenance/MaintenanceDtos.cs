namespace Dairy.Application.DTOs.Maintenance;

public class EquipmentDto
{
    public long Id { get; set; }
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
    public string? BranchName { get; set; }
    public long? CollectionCenterId { get; set; }
    public string? CollectionCenterName { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string? VendorName { get; set; }

    /// <summary>Operational | UnderMaintenance | Retired</summary>
    public string Status { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }

    /// <summary>Earliest NextDueDate over the equipment's active schedules.</summary>
    public DateTime? NextDueDate { get; set; }

    public DateTime? LastMaintenanceDate { get; set; }
}

public class MaintenanceScheduleDto
{
    public long Id { get; set; }
    public long EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int FrequencyDays { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public int AlertBeforeDays { get; set; }
    public long? AssignedEmployeeId { get; set; }
    public string? AssignedEmployeeName { get; set; }

    /// <summary>Active | Paused</summary>
    public string Status { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public int DaysToDue { get; set; }
    public bool IsOverdue { get; set; }

    /// <summary>Active and NextDueDate is within AlertBeforeDays (or past).</summary>
    public bool IsDue { get; set; }
}

public class MaintenanceRecordDto
{
    public long Id { get; set; }
    public long EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public long? MaintenanceScheduleId { get; set; }
    public string? TaskName { get; set; }

    /// <summary>Preventive | Corrective | Breakdown</summary>
    public string MaintenanceType { get; set; } = string.Empty;

    public DateTime MaintenanceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public decimal Cost { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string? PerformedBy { get; set; }
    public decimal DowntimeHours { get; set; }
    public string? InvoiceNo { get; set; }

    /// <summary>Completed | InProgress</summary>
    public string Status { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public long? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>Result row of sp_RecordMaintenance.</summary>
public class MaintenanceRecordedDto
{
    public long Id { get; set; }
    public DateTime? NextDueDate { get; set; }
    public long? ExpenseId { get; set; }
}

/// <summary>Row of sp_GetMaintenanceDue.</summary>
public class MaintenanceDueDto
{
    /// <summary>Schedule id.</summary>
    public long Id { get; set; }
    public long EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string? Location { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public int FrequencyDays { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public int DaysToDue { get; set; }
    public bool IsOverdue { get; set; }
    public long? AssignedEmployeeId { get; set; }
    public string? AssignedEmployeeName { get; set; }
}
