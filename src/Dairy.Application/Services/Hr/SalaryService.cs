using Dairy.Application.Common;
using Dairy.Application.DTOs.Hr;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Hr;

public class SalaryService : ISalaryService
{
    private readonly ISalaryRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public SalaryService(ISalaryRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public Task<PagedResult<SalaryDto>> GetPagedAsync(SalaryQuery query, CancellationToken ct = default) =>
        _repo.GetPagedAsync(query, ct);

    public async Task<SalaryDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Salary", id);

    public async Task<SalaryGenerateResultDto> GenerateAsync(GenerateSalariesRequest request, CancellationToken ct = default)
    {
        var result = await _repo.GenerateAsync(request.Month, request.Year, _user.UserId, ct);
        await _audit.LogAsync(AuditActions.Create, "Salary", $"{request.Year}-{request.Month:00}", null, result, ct);
        return result;
    }

    public async Task<SalaryDto> PayAsync(long id, PaySalaryRequest request, CancellationToken ct = default)
    {
        var before = await GetByIdAsync(id, ct);
        await _repo.PayAsync(id, request.PaymentModeId, _user.UserId, ct);
        var after = await GetByIdAsync(id, ct);
        await _audit.LogAsync(AuditActions.Payment, "Salary", id, before, after, ct);
        return after;
    }
}
