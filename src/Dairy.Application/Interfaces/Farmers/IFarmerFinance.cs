using Dairy.Application.Common;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Requests.Farmers;

namespace Dairy.Application.Interfaces.Farmers;

public interface IFarmerAdvanceRepository
{
    Task<PagedResult<FarmerAdvanceDto>> GetPagedAsync(FarmerAdvanceQuery query, CancellationToken ct = default);
    Task<FarmerAdvanceDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(CreateFarmerAdvanceRequest request, DateTime advanceDate, long userId, CancellationToken ct = default);
}

public interface IFarmerAdvanceService
{
    Task<PagedResult<FarmerAdvanceDto>> GetPagedAsync(FarmerAdvanceQuery query, CancellationToken ct = default);
    Task<FarmerAdvanceDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<FarmerAdvanceDto> CreateAsync(CreateFarmerAdvanceRequest request, CancellationToken ct = default);
}

public interface IFarmerLoanRepository
{
    Task<PagedResult<FarmerLoanListDto>> GetPagedAsync(FarmerLoanQuery query, CancellationToken ct = default);
    Task<FarmerLoanDto?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Calls sp_CreateFarmerLoan; returns the new loan id.</summary>
    Task<long> CreateAsync(CreateFarmerLoanRequest request, DateTime startDate, long userId, CancellationToken ct = default);

    /// <summary>Calls sp_PayLoanInstallment.</summary>
    Task PayInstallmentAsync(long loanId, int installmentNo, decimal amount, long userId, CancellationToken ct = default);
}

public interface IFarmerLoanService
{
    Task<PagedResult<FarmerLoanListDto>> GetPagedAsync(FarmerLoanQuery query, CancellationToken ct = default);
    Task<FarmerLoanDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<FarmerLoanDto> CreateAsync(CreateFarmerLoanRequest request, CancellationToken ct = default);
    Task<FarmerLoanDto> PayInstallmentAsync(long loanId, int installmentNo, PayLoanInstallmentRequest? request, CancellationToken ct = default);
}
