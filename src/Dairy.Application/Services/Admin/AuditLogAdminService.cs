using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;

namespace Dairy.Application.Services.Admin;

public class AuditLogAdminService : IAuditLogAdminService
{
    private readonly IAuditLogAdminRepository _repo;

    public AuditLogAdminService(IAuditLogAdminRepository repo) => _repo = repo;

    public Task<PagedResult<AuditLogListDto>> GetPagedAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        if (query.From.HasValue && query.To.HasValue && query.From.Value.Date > query.To.Value.Date)
            throw new RequestValidationException("from", "'From' date must be on or before the 'To' date.");
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<AuditLogDto> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Audit log entry", id);

    public Task<AuditFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default) =>
        _repo.GetFilterOptionsAsync(ct);
}
