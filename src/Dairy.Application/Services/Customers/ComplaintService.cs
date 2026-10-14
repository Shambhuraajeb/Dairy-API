using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Customers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Customers;
using Dairy.Application.Requests.Customers;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Customers;

public class ComplaintService : IComplaintService
{
    private readonly IComplaintRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public ComplaintService(IComplaintRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    private bool IsCustomerUser => Roles.IsCustomerRole(_user.Role);

    public Task<PagedResult<ComplaintDto>> GetPagedAsync(ComplaintQuery query, CancellationToken ct = default)
    {
        if (IsCustomerUser) query.CustomerId = _user.CustomerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<ComplaintDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);
        if (IsCustomerUser && dto.CustomerId != _user.CustomerId) throw new NotFoundException("Complaint", id);
        return dto;
    }

    public async Task<ComplaintDto> CreateAsync(CreateComplaintRequest request, CancellationToken ct = default)
    {
        long customerId;
        if (IsCustomerUser)
            customerId = _user.CustomerId ?? throw new ForbiddenException();
        else
            customerId = request.CustomerId ?? throw new RequestValidationException("customerId", "Customer is required.");

        if (!await _repo.CustomerExistsAsync(customerId, ct))
            throw new NotFoundException("Customer", customerId);
        if (request.SalesOrderId.HasValue && !await _repo.OrderBelongsToCustomerAsync(request.SalesOrderId.Value, customerId, ct))
            throw new BusinessRuleException("The selected order does not belong to this customer.");
        if (request.DeliveryId.HasValue && !await _repo.DeliveryBelongsToCustomerAsync(request.DeliveryId.Value, customerId, ct))
            throw new BusinessRuleException("The selected delivery does not belong to this customer.");

        var created = await _repo.CreateAsync(customerId, request, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(created.Id, ct) ?? throw new NotFoundException("Complaint", created.Id);

        await _audit.LogAsync(AuditActions.Create, "CustomerComplaint", created.Id, null, dto, ct);
        return dto;
    }

    public async Task<ComplaintDto> AssignAsync(long id, AssignComplaintRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);
        EnsureOpenForWork(existing);
        if (!await _repo.UserExistsAsync(request.AssignedTo, ct))
            throw new BusinessRuleException("The selected user does not exist or is inactive.");

        await _repo.AssignAsync(id, request.AssignedTo, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);

        await _repo.AddNotificationAsync(request.AssignedTo, "Complaint assigned",
            $"Complaint {dto.ComplaintNumber} has been assigned to you.", NotificationSeverity.Info, "/customers/complaints", ct);
        await _audit.LogAsync("Assign", "CustomerComplaint", id, existing, dto, ct);
        return dto;
    }

    public async Task<ComplaintDto> SetStatusAsync(long id, ComplaintStatusRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);
        if (existing.Status == ComplaintStatus.Closed)
            throw new BusinessRuleException("A closed complaint cannot be changed.");

        var status = request.Status.Trim();
        if (status == ComplaintStatus.Assigned && request.AssignedTo is null && existing.AssignedTo is null)
            throw new BusinessRuleException("Use the assign action (or send assignedTo) to move a complaint to Assigned.");
        if (request.AssignedTo.HasValue && !await _repo.UserExistsAsync(request.AssignedTo.Value, ct))
            throw new BusinessRuleException("The selected user does not exist or is inactive.");

        await _repo.SetStatusAsync(id, status, request.AssignedTo, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);

        await _audit.LogAsync(AuditActions.Update, "CustomerComplaint", id, existing, dto, ct);
        return dto;
    }

    public async Task<ComplaintDto> ResolveAsync(long id, ResolveComplaintRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);
        EnsureOpenForWork(existing);

        await _repo.ResolveAsync(id, request.Resolution.Trim(), _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);

        await _audit.LogAsync("Resolve", "CustomerComplaint", id, existing, dto, ct);
        return dto;
    }

    public async Task<ComplaintDto> CloseAsync(long id, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);
        if (existing.Status == ComplaintStatus.Closed)
            throw new BusinessRuleException("The complaint is already closed.");
        if (existing.Status != ComplaintStatus.Resolved)
            throw new BusinessRuleException("Only a resolved complaint can be closed. Resolve it first.");

        await _repo.CloseAsync(id, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);

        await _audit.LogAsync("Close", "CustomerComplaint", id, existing, dto, ct);
        return dto;
    }

    public async Task<ComplaintDto> FeedbackAsync(long id, ComplaintFeedbackRequest request, CancellationToken ct = default)
    {
        if (!IsCustomerUser) throw new ForbiddenException("Only the customer who raised the complaint can leave feedback.");

        var existing = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);
        if (existing.CustomerId != _user.CustomerId) throw new NotFoundException("Complaint", id);
        if (existing.Status != ComplaintStatus.Resolved && existing.Status != ComplaintStatus.Closed)
            throw new BusinessRuleException("Feedback can only be given after the complaint is resolved.");
        if (existing.FeedbackRating.HasValue)
            throw new BusinessRuleException("Feedback has already been submitted for this complaint.");

        await _repo.SaveFeedbackAsync(id, request.Rating, request.Comments, _user.UserId, ct);
        var dto = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Complaint", id);

        await _audit.LogAsync("Feedback", "CustomerComplaint", id, existing, dto, ct);
        return dto;
    }

    private static void EnsureOpenForWork(ComplaintDto complaint)
    {
        if (complaint.Status == ComplaintStatus.Closed)
            throw new BusinessRuleException("A closed complaint cannot be changed.");
        if (complaint.Status == ComplaintStatus.Resolved)
            throw new BusinessRuleException("The complaint is already resolved. Reopen it by setting the status to InProgress.");
    }
}
