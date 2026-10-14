using Dairy.Application.Common;
using Dairy.Application.DTOs.Finance;
using Dairy.Application.Interfaces.Finance;
using Dairy.Application.Requests.Finance;

namespace Dairy.Application.Services.Finance;

public class CashTransactionService : ICashTransactionService
{
    private readonly ICashTransactionRepository _repo;

    public CashTransactionService(ICashTransactionRepository repo) => _repo = repo;

    public Task<PagedResult<CashTransactionDto>> GetPagedAsync(CashTransactionQuery query, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(query.Type) &&
            !string.Equals(query.Type, "In", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(query.Type, "Out", StringComparison.OrdinalIgnoreCase))
            throw new RequestValidationException("type", "Type must be In or Out.");

        query.Type = string.IsNullOrWhiteSpace(query.Type)
            ? null
            : (string.Equals(query.Type, "In", StringComparison.OrdinalIgnoreCase) ? "In" : "Out");
        query.Category = TextClean.Opt(query.Category);
        return _repo.GetPagedAsync(query, ct);
    }

    public Task<CashSummaryDto> GetSummaryAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            throw new RequestValidationException("from", "'from' cannot be after 'to'.");
        return _repo.GetSummaryAsync(from, to, ct);
    }
}
