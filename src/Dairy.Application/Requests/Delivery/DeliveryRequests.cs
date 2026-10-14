using Dairy.Application.Common;

namespace Dairy.Application.Requests.Delivery;

public class RouteQuery : PagedQuery
{
    public long? BranchId { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateRouteRequest
{
    /// <summary>Optional; auto-generated (RT004 ...) when blank.</summary>
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? BranchId { get; set; }
    public string? Description { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public decimal DistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public long? DefaultVehicleId { get; set; }
    public long? DefaultDriverEmployeeId { get; set; }
}

public class UpdateRouteRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? BranchId { get; set; }
    public string? Description { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public decimal DistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public long? DefaultVehicleId { get; set; }
    public long? DefaultDriverEmployeeId { get; set; }

    /// <summary>Null = unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class VehicleQuery : PagedQuery
{
    /// <summary>Available | OnRoute | Maintenance | Inactive.</summary>
    public string? Status { get; set; }
    public string? VehicleType { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateVehicleRequest
{
    public string RegistrationNo { get; set; } = string.Empty;

    /// <summary>Van | Truck | Tanker | Bike | Auto.</summary>
    public string VehicleType { get; set; } = "Van";
    public string? Make { get; set; }
    public string? Model { get; set; }
    public decimal Capacity { get; set; }
    public string CapacityUnit { get; set; } = "Liters";
    public long? DriverEmployeeId { get; set; }
    public long? BranchId { get; set; }
    public string? FuelType { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public DateTime? FitnessExpiry { get; set; }

    /// <summary>Available | OnRoute | Maintenance | Inactive.</summary>
    public string Status { get; set; } = "Available";
}

public class UpdateVehicleRequest
{
    public string RegistrationNo { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "Van";
    public string? Make { get; set; }
    public string? Model { get; set; }
    public decimal Capacity { get; set; }
    public string CapacityUnit { get; set; } = "Liters";
    public long? DriverEmployeeId { get; set; }
    public long? BranchId { get; set; }
    public string? FuelType { get; set; }
    public DateTime? InsuranceExpiry { get; set; }
    public DateTime? FitnessExpiry { get; set; }
    public string Status { get; set; } = "Available";

    /// <summary>Null = unchanged; false = deactivate.</summary>
    public bool? IsActive { get; set; }
}

public class DeliveryScheduleQuery : PagedQuery
{
    /// <summary>Exact schedule date.</summary>
    public DateTime? Date { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public long? RouteId { get; set; }

    /// <summary>Planned | Dispatched | Completed.</summary>
    public string? Status { get; set; }
    public string? Slot { get; set; }
    public long? DriverEmployeeId { get; set; }
}

public class CreateDeliveryScheduleRequest
{
    public long RouteId { get; set; }

    /// <summary>Null = the route's default vehicle.</summary>
    public long? VehicleId { get; set; }

    /// <summary>Null = the route's default driver.</summary>
    public long? DriverEmployeeId { get; set; }
    public DateTime ScheduleDate { get; set; }

    /// <summary>Morning | Evening.</summary>
    public string Slot { get; set; } = "Morning";

    /// <summary>HH:mm.</summary>
    public string? PlannedStartTime { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>Route cannot change. ScheduleDate/Slot can only change while the schedule is Planned.</summary>
public class UpdateDeliveryScheduleRequest
{
    public long? VehicleId { get; set; }
    public long? DriverEmployeeId { get; set; }
    public DateTime ScheduleDate { get; set; }
    public string Slot { get; set; } = "Morning";
    public string? PlannedStartTime { get; set; }
    public string? Remarks { get; set; }
}

public class DeliveryQuery : PagedQuery
{
    /// <summary>Delivery date of the order (query key: date).</summary>
    public DateTime? Date { get; set; }

    /// <summary>Pending | Dispatched | Delivered | Failed | Cancelled.</summary>
    public string? Status { get; set; }
    public long? RouteId { get; set; }
    public long? DriverEmployeeId { get; set; }
    public long? ScheduleId { get; set; }
    public string? Slot { get; set; }
    public long? CustomerId { get; set; }

    /// <summary>Set by the service for the Driver role; ignored from the client.</summary>
    public long? ScopeUserId { get; set; }

    /// <summary>Set by the service for the Driver role; ignored from the client.</summary>
    public long? ScopeEmployeeId { get; set; }
}

public class CreateDeliveryRequest
{
    public long SalesOrderId { get; set; }

    /// <summary>Optional schedule to attach to (also supplies route and default driver).</summary>
    public long? DeliveryScheduleId { get; set; }
    public long? DriverEmployeeId { get; set; }

    /// <summary>User allowed to confirm; null = the driver's user.</summary>
    public long? AssignedUserId { get; set; }
}

public class BulkDeliveryRequest
{
    public long DeliveryScheduleId { get; set; }
    public List<long> SalesOrderIds { get; set; } = new();
    public long? DriverEmployeeId { get; set; }
    public long? AssignedUserId { get; set; }
}

public class ConfirmDeliveryRequest
{
    /// <summary>true (default) = delivered, false = failed (failureReason required).</summary>
    public bool IsDelivered { get; set; } = true;
    public string? ReceivedBy { get; set; }
    public string? Remarks { get; set; }
    public string? FailureReason { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
