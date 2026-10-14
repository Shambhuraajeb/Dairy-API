using Dairy.Application.Common;
using Dairy.Application.DTOs.Billing;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Billing;
using Dairy.Application.Requests.Billing;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Billing;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public InvoiceService(IInvoiceRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    private bool IsCustomerUser => Roles.IsCustomerRole(_user.Role);

    public Task<PagedResult<InvoiceListDto>> GetPagedAsync(InvoiceQuery query, CancellationToken ct = default)
    {
        if (IsCustomerUser) query.CustomerId = _user.CustomerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<InvoiceDetailDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Invoice", id);
        if (IsCustomerUser && dto.CustomerId != _user.CustomerId) throw new NotFoundException("Invoice", id);
        return dto;
    }

    public async Task<InvoiceDetailDto> GenerateAsync(GenerateInvoiceRequest request, CancellationToken ct = default)
    {
        var created = await _repo.GenerateAsync(request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Invoice", created.Id);

        await _audit.LogAsync(AuditActions.Create, "Invoice", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<InvoiceDetailDto> CancelAsync(long id, CancelInvoiceRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);

        if (existing.Status == "Cancelled")
            throw new BusinessRuleException("The invoice is already cancelled.");
        if (existing.PaidAmount > 0 || existing.Payments.Count > 0 || await _repo.CountAllocationsAsync(id, ct) > 0)
            throw new BusinessRuleException("An invoice with payments cannot be cancelled.");
        if (existing.Status != "Issued")
            throw new BusinessRuleException($"Only an issued invoice can be cancelled (this one is {existing.Status}).");

        if (!await _repo.CancelAsync(id, request.Reason, _user.UserId, ct))
            throw new BusinessRuleException("The invoice state changed (a payment may have been recorded) and it can no longer be cancelled.");

        var dto = await GetByIdAsync(id, ct);
        await _audit.LogAsync("Cancel", "Invoice", id, existing, dto, ct);
        return dto;
    }
}
