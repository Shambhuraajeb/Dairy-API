using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.DTOs.Orders;
using Dairy.Application.Requests.Delivery;
using Dairy.Application.Requests.Orders;

namespace Dairy.Application.Interfaces.Delivery;

public interface IRouteRepository
{
    Task<PagedResult<RouteDto>> GetPagedAsync(RouteQuery query, CancellationToken ct = default);
    Task<RouteDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(string? code, CreateRouteRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateRouteRequest request, bool isActive, long userId, CancellationToken ct = default);
    Task<int> CountOpenSchedulesAsync(long id, CancellationToken ct = default);
    Task SetActiveAsync(long id, bool isActive, long userId, CancellationToken ct = default);
}

public interface IRouteService
{
    Task<PagedResult<RouteDto>> GetPagedAsync(RouteQuery query, CancellationToken ct = default);
    Task<RouteDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<RouteDto> CreateAsync(CreateRouteRequest request, CancellationToken ct = default);
    Task<RouteDto> UpdateAsync(long id, UpdateRouteRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IVehicleRepository
{
    Task<PagedResult<VehicleDto>> GetPagedAsync(VehicleQuery query, CancellationToken ct = default);
    Task<VehicleDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> RegistrationExistsAsync(string registrationNo, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(string registrationNo, CreateVehicleRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, string registrationNo, UpdateVehicleRequest request, bool isActive, long userId, CancellationToken ct = default);
    Task<int> CountOpenSchedulesAsync(long id, CancellationToken ct = default);
    Task SetInactiveAsync(long id, long userId, CancellationToken ct = default);
}

public interface IVehicleService
{
    Task<PagedResult<VehicleDto>> GetPagedAsync(VehicleQuery query, CancellationToken ct = default);
    Task<VehicleDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default);
    Task<VehicleDto> UpdateAsync(long id, UpdateVehicleRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IDeliveryScheduleRepository
{
    Task<PagedResult<DeliveryScheduleDto>> GetPagedAsync(DeliveryScheduleQuery query, CancellationToken ct = default);
    Task<DeliveryScheduleDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> RouteActiveAsync(long routeId, CancellationToken ct = default);
    Task<bool> SlotTakenAsync(long routeId, DateTime date, string slot, long? excludeId, CancellationToken ct = default);
    Task<long> CreateAsync(CreateDeliveryScheduleRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateDeliveryScheduleRequest request, bool driverChanged, long userId, CancellationToken ct = default);
    Task<int> CountLiveDeliveriesAsync(long id, CancellationToken ct = default);
    Task DeactivateAsync(long id, long userId, CancellationToken ct = default);
}

public interface IDeliveryScheduleService
{
    Task<PagedResult<DeliveryScheduleDto>> GetPagedAsync(DeliveryScheduleQuery query, CancellationToken ct = default);
    Task<DeliveryScheduleDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<DeliveryScheduleDto> CreateAsync(CreateDeliveryScheduleRequest request, CancellationToken ct = default);
    Task<DeliveryScheduleDto> UpdateAsync(long id, UpdateDeliveryScheduleRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IDeliveryRepository
{
    Task<PagedResult<DeliveryListDto>> GetPagedAsync(DeliveryQuery query, CancellationToken ct = default);
    Task<List<DeliveryListDto>> GetMyAsync(long userId, long? employeeId, DateTime date, CancellationToken ct = default);
    Task<DeliveryDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CreatedResult> CreateAsync(CreateDeliveryRequest request, long userId, CancellationToken ct = default);
    Task<BulkDeliveryResultDto> CreateBulkAsync(BulkDeliveryRequest request, long userId, CancellationToken ct = default);
    Task DispatchAsync(long id, long userId, CancellationToken ct = default);
    Task ConfirmAsync(long id, ConfirmDeliveryRequest request, bool isPrivileged, long userId, CancellationToken ct = default);
}

public interface IDeliveryService
{
    Task<PagedResult<DeliveryListDto>> GetPagedAsync(DeliveryQuery query, CancellationToken ct = default);
    Task<List<DeliveryListDto>> GetMyAsync(DateTime? date, CancellationToken ct = default);
    Task<PagedResult<OrderListDto>> GetPendingOrdersAsync(OrderQuery query, CancellationToken ct = default);
    Task<DeliveryDetailDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<DeliveryDetailDto> CreateAsync(CreateDeliveryRequest request, CancellationToken ct = default);
    Task<BulkDeliveryResultDto> CreateBulkAsync(BulkDeliveryRequest request, CancellationToken ct = default);
    Task<DeliveryDetailDto> DispatchAsync(long id, CancellationToken ct = default);
    Task<DeliveryDetailDto> ConfirmAsync(long id, ConfirmDeliveryRequest? request, CancellationToken ct = default);
}
