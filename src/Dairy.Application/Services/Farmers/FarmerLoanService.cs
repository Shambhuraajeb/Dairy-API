using Dairy.Application.Common;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Farmers;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Farmers;

/// <summary>Loan creation and installment payment run in stored procedures, which also write the audit rows.</summary>
public class FarmerLoanService : IFarmerLoanService
{
    private readonly IFarmerLoanRepository _repo;
    private readonly IFarmerRepository _farmers;
    private readonly ICurrentUser _user;

    public FarmerLoanService(IFarmerLoanRepository repo, IFarmerRepository farmers, ICurrentUser user)
    {
        _repo = repo;
        _farmers = farmers;
        _user = user;
    }

    public Task<PagedResult<FarmerLoanListDto>> GetPagedAsync(FarmerLoanQuery query, CancellationToken ct = default)
    {
        if (_user.IsInRole(Roles.Farmer))
            query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();
        return _repo.GetPagedAsync(query, ct);
    }

    public async Task<FarmerLoanDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var loan = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Loan", id);
        if (_user.IsInRole(Roles.Farmer) && loan.FarmerId != _user.FarmerId)
            throw new NotFoundException("Loan", id);
        return loan;
    }

    public async Task<FarmerLoanDto> CreateAsync(CreateFarmerLoanRequest request, CancellationToken ct = default)
    {
        var farmer = await _farmers.GetBasicAsync(request.FarmerId, ct) ?? throw new NotFoundException("Farmer", request.FarmerId);
        if (farmer.Status != FarmerStatus.Active)
            throw new BusinessRuleException("Loans can only be given to active farmers.");

        var startDate = (request.StartDate ?? DateTime.Today).Date;
        var id = await _repo.CreateAsync(request, startDate, _user.UserId, ct);
        return await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Loan", id);
    }

    public async Task<FarmerLoanDto> PayInstallmentAsync(long loanId, int installmentNo, PayLoanInstallmentRequest? request, CancellationToken ct = default)
    {
        var loan = await GetByIdAsync(loanId, ct);
        if (loan.Status != LoanStatus.Active)
            throw new BusinessRuleException("This loan is not active.");

        var installment = loan.Installments.FirstOrDefault(i => i.InstallmentNo == installmentNo)
                          ?? throw new NotFoundException("Installment", installmentNo);
        if (installment.Status == InstallmentStatus.Paid)
            throw new BusinessRuleException("This installment is already paid.");

        var balance = installment.Amount - installment.PaidAmount;
        var amount = request?.Amount ?? balance;
        if (amount <= 0)
            throw new BusinessRuleException("Amount must be greater than zero.");
        if (amount > balance)
            throw new BusinessRuleException($"Amount cannot exceed the installment balance ({balance:0.00}).");

        await _repo.PayInstallmentAsync(loanId, installmentNo, amount, _user.UserId, ct);
        return await _repo.GetByIdAsync(loanId, ct) ?? throw new NotFoundException("Loan", loanId);
    }
}
