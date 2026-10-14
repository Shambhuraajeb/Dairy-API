using Dairy.Application.Common;
using Dairy.Application.DTOs.Delivery;
using Dairy.Application.DTOs.Orders;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Delivery;
using Dairy.Application.Interfaces.Orders;
using Dairy.Application.Requests.Delivery;
using Dairy.Application.Requests.Orders;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Delivery;

public class DeliveryService : IDeliveryService
{
    private readonly IDeliveryRepository _repo;
    private readonly IOrderService _orders;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public DeliveryService(IDeliveryRepository repo, IOrderService orders, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _orders = orders;
        _audit = audit;
        _user = user;
    }

    private bool IsDriver => _user.IsInRole(Roles.Driver);

    /// <summary>A driver owns a delivery when it is assigned to their login, or to their employee record with no explicit user.</summary>
    private bool DriverOwns(DeliveryListDto d) =>
        d.AssignedUserId == _user.UserId ||
        (d.AssignedUserId is null && _user.EmployeeId.HasValue && d.DriverEmployeeId == _user.EmployeeId);

    public Task<PagedResult<DeliveryListDto>> GetPagedAsync(DeliveryQuery query, CancellationToken ct = default)
    {
        if (IsDriver)
        {
            query.ScopeUserId = _user.UserId;
            query.ScopeEmployeeId = _user.EmployeeId;
        }
        else
        {
            query.ScopeUserId = null;
            query.ScopeEmployeeId = null;
        }
        return _repo.GetPagedAsync(query, ct);
    }

    public Task<List<DeliveryListDto>> GetMyAsync(DateTime? date, CancellationToken ct = default) =>
        _repo.GetMyAsync(_user.UserId, _user.EmployeeId, (date ?? DateTime.Today).Date, ct);

    public Task<PagedResult<OrderListDto>> GetPendingOrdersAsync(OrderQuery query, CancellationToken ct = default) =>
        _orders.GetAwaitingSchedulingAsync(query, ct);

    public async Task<DeliveryDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Delivery", id);
        if (IsDriver && !DriverOwns(dto)) throw new NotFoundException("Delivery", id);
        return dto;
    }

    public async Task<DeliveryDetailDto> CreateAsync(CreateDeliveryRequest request, CancellationToken ct = default)
    {
        var created = await _repo.CreateAsync(request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Delivery", created.Id);

        await _audit.LogAsync(AuditActions.Create, "Delivery", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<BulkDeliveryResultDto> CreateBulkAsync(BulkDeliveryRequest request, CancellationToken ct = default)
    {
        var result = await _repo.CreateBulkAsync(request, _user.UserId, ct);

        foreach (var created in result.Created)
            await _audit.LogAsync(AuditActions.Create, "Delivery", created.DeliveryId, null,
                new { created.SalesOrderId, created.DeliveryNumber, request.DeliveryScheduleId, Bulk = true }, ct);
        return result;
    }

    public async Task<DeliveryDetailDto> DispatchAsync(long id, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct); // drivers can only dispatch their own stops

        await _repo.DispatchAsync(id, _user.UserId, ct);
        var dto = await GetByIdAsync(id, ct);

        await _audit.LogAsync("Dispatch", "Delivery", id, existing, dto, ct);
        return dto;
    }

    public async Task<DeliveryDetailDto> ConfirmAsync(long id, ConfirmDeliveryRequest? request, CancellationToken ct = default)
    {
        request ??= new ConfirmDeliveryRequest();
        var existing = await GetByIdAsync(id, ct); // drivers only reach their own deliveries

        if (!request.IsDelivered && string.IsNullOrWhiteSpace(request.FailureReason))
            throw new RequestValidationException("failureReason", "A failure reason is required when the delivery was not completed.");

        // Managers (delivery.update) may confirm any delivery; a driver may only confirm a delivery assigned to them,
        // which the stored procedure enforces via AssignedUserId (privileged = false).
        var privileged = _user.HasPermission(Permissions.DeliveryUpdate) && !IsDriver;
        if (IsDriver && existing.AssignedUserId is null && DriverOwns(existing))
            privileged = true; // assigned by employee only: the driver's own login is the authorized confirmer

        await _repo.ConfirmAsync(id, request, privileged, _user.UserId, ct);
        var dto = await GetByIdAsync(id, ct);

        await _audit.LogAsync("Confirm", "Delivery", id, existing, dto, ct);
        return dto;
    }
}
