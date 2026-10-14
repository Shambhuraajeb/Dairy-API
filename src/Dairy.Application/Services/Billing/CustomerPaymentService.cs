using Dairy.Application.Common;
using Dairy.Application.DTOs.Billing;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Billing;
using Dairy.Application.Requests.Billing;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Billing;

public class CustomerPaymentService : ICustomerPaymentService
{
    private readonly ICustomerPaymentRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public CustomerPaymentService(ICustomerPaymentRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    private bool IsCustomerUser => Roles.IsCustomerRole(_user.Role);

    public Task<PagedResult<CustomerPaymentListDto>> GetPagedAsync(CustomerPaymentQuery query, CancellationToken ct = default)
    {
        if (IsCustomerUser) query.CustomerId = _user.CustomerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<CustomerPaymentDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Payment", id);
        if (IsCustomerUser && dto.CustomerId != _user.CustomerId) throw new NotFoundException("Payment", id);
        return dto;
    }

    public async Task<CustomerPaymentDetailDto> RecordAsync(RecordCustomerPaymentRequest request, CancellationToken ct = default)
    {
        // allocation, invoice status/balances, customer outstanding and the cash book are all handled atomically by the procedure
        var created = await _repo.RecordAsync(request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Payment", created.Id);

        await _audit.LogAsync(AuditActions.Payment, "CustomerPayment", created.Id, null, dto, ct);
        return dto;
    }
}
