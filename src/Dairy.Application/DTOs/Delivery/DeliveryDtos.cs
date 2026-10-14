using Dairy.Application.DTOs.Orders;

namespace Dairy.Application.DTOs.Delivery;

public class RouteDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Description { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public decimal DistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public long? DefaultVehicleId { get; set; }
    public string? DefaultVehicleNo { get; set; }
    public long? DefaultDriverEmployeeId { get; set; }
    public string? DefaultDriverName { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Active customer addresses attached to this route.</summary>
    public int AddressCount { get; set; }
}

public class VehicleDto
{
    public long Id { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;

    /// <summary>Van | Truck | Tanker | Bike | Auto.</summary>
    public string VehicleType { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public decimal Capacity { get; set; }
    public string CapacityUnit { get; set; } = string.Empty;
    public long? DriverEmployeeId { get; set; }
    public string? DriverName { get; set; }
    public long? BranchId { get; set; }
    public string? FuelType { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public DateTime? FitnessExpiry { get; set; }

    /// <summary>Available | OnRoute | Maintenance | Inactive.</summary>
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class DeliveryScheduleDto
{
    public long Id { get; set; }
    public long RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public long? VehicleId { get; set; }
    public string? VehicleNo { get; set; }
    public long? DriverEmployeeId { get; set; }
    public string? DriverName { get; set; }
    public DateTime ScheduleDate { get; set; }

    /// <summary>Morning | Evening.</summary>
    public string Slot { get; set; } = string.Empty;

    /// <summary>Planned | Dispatched | Completed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>HH:mm or null.</summary>
    public string? PlannedStartTime { get; set; }
    public DateTime? DispatchedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Remarks { get; set; }
    public int DeliveryCount { get; set; }
    public int DeliveredCount { get; set; }
    public bool IsActive { get; set; }
}

public class DeliveryListDto
{
    public long Id { get; set; }
    public string DeliveryNumber { get; set; } = string.Empty;
    public long SalesOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerMobile { get; set; }
    public string? AddressText { get; set; }
    public string? DeliveryInstructions { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string DeliverySlot { get; set; } = string.Empty;
    public long? DeliveryScheduleId { get; set; }
    public long? RouteId { get; set; }
    public string? RouteName { get; set; }
    public long? DriverEmployeeId { get; set; }
    public string? DriverName { get; set; }
    public long? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public int SequenceNo { get; set; }

    /// <summary>Pending | Dispatched | Delivered | Failed | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime? DispatchedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? FailureReason { get; set; }
    public decimal OrderTotal { get; set; }
    public decimal TotalQuantity { get; set; }
    public long? InvoiceId { get; set; }
    public string? Remarks { get; set; }
}

public class DeliveryDetailDto : DeliveryListDto
{
    public List<OrderLineDto> Lines { get; set; } = new();
    public DeliveryConfirmationDto? Confirmation { get; set; }
}

public class DeliveryConfirmationDto
{
    public long Id { get; set; }
    public long DeliveryId { get; set; }
    public long ConfirmedBy { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime ConfirmedDate { get; set; }

    /// <summary>Delivered | Partial | Failed.</summary>
    public string Outcome { get; set; } = string.Empty;
    public decimal DeliveredQuantity { get; set; }
    public string? ReceivedBy { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? FailureReason { get; set; }
    public string? Remarks { get; set; }
}

public class BulkDeliveryResultDto
{
    public int RequestedCount { get; set; }
    public int CreatedCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkDeliveryCreatedDto> Created { get; set; } = new();
    public List<BulkDeliveryFailedDto> Failed { get; set; } = new();
}

public class BulkDeliveryCreatedDto
{
    public long SalesOrderId { get; set; }
    public long DeliveryId { get; set; }
    public string? DeliveryNumber { get; set; }
}

public class BulkDeliveryFailedDto
{
    public long SalesOrderId { get; set; }
    public string Message { get; set; } = string.Empty;
}
