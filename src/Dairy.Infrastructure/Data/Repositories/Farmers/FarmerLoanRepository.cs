using Dairy.Application.Common;
using Dairy.Application.DTOs.Farmers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Farmers;
using Dairy.Application.Requests.Farmers;

namespace Dairy.Infrastructure.Data.Repositories.Farmers;

public class FarmerLoanRepository : IFarmerLoanRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "l.FarmerLoanId",
        ["loanNumber"] = "l.LoanNumber",
        ["loanDate"] = "l.LoanDate",
        ["farmerName"] = "f.FullName",
        ["principalAmount"] = "l.PrincipalAmount",
        ["outstandingAmount"] = "l.OutstandingAmount",
        ["status"] = "l.Status"
    };

    private const string From = " FROM `FarmerLoan` l JOIN `Farmer` f ON f.FarmerId = l.FarmerId";

    private const string ListColumns =
        "l.FarmerLoanId AS Id, l.LoanNumber, l.FarmerId, f.FarmerCode, f.FullName AS FarmerName, l.LoanDate, " +
        "l.PrincipalAmount, l.InterestRate, l.InterestAmount, l.TotalPayable, l.NumberOfInstallments, l.InstallmentAmount, " +
        "l.FirstInstallmentDate, l.PaidAmount, l.OutstandingAmount, l.Status, l.Purpose, " +
        "(SELECT COUNT(*) FROM `FarmerLoanInstallment` i1 WHERE i1.FarmerLoanId = l.FarmerLoanId AND i1.IsActive = 1 AND i1.Status = 'Paid') AS PaidInstallments, " +
        "(SELECT MIN(i2.DueDate) FROM `FarmerLoanInstallment` i2 WHERE i2.FarmerLoanId = l.FarmerLoanId AND i2.IsActive = 1 AND i2.Status <> 'Paid') AS NextDueDate";

    private const string Where =
        " WHERE l.IsActive = 1" +
        " AND (@Search IS NULL OR l.LoanNumber LIKE @Search OR f.FullName LIKE @Search OR f.FarmerCode LIKE @Search)" +
        " AND (@FarmerId IS NULL OR l.FarmerId = @FarmerId)" +
        " AND (@Status IS NULL OR l.Status = @Status)";

    private const string DetailSelect =
        "SELECT " + ListColumns + ", l.InterestType, l.PaymentModeId, pm.Name AS PaymentModeName, l.ReferenceNo, " +
        "l.ApprovedDate, l.ClosedDate, l.Remarks, l.CreatedDate" + From +
        " LEFT JOIN `PaymentMode` pm ON pm.PaymentModeId = l.PaymentModeId";

    private const string InstallmentSelect =
        "SELECT i.FarmerLoanInstallmentId AS Id, i.FarmerLoanId, i.InstallmentNo, i.DueDate, i.Amount, i.PrincipalComponent, " +
        "i.InterestComponent, i.PaidAmount, (i.Amount - i.PaidAmount) AS BalanceAmount, i.PaidDate, i.Status, " +
        "(i.Status <> 'Paid' AND i.DueDate < CURDATE()) AS IsOverdue, i.PaymentSource, i.FarmerPaymentId, i.Remarks " +
        "FROM `FarmerLoanInstallment` i WHERE i.FarmerLoanId = @LoanId AND i.IsActive = 1 ORDER BY i.InstallmentNo";

    private readonly IDbConnectionFactory _db;

    public FarmerLoanRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<FarmerLoanListDto>> GetPagedAsync(FarmerLoanQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.FarmerId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim()
        };
        return await conn.QueryPagedAsync<FarmerLoanListDto>(
            "SELECT " + ListColumns + From + Where,
            "SELECT COUNT(*)" + From + Where,
            param, query, SortColumns, "l.LoanDate DESC, l.FarmerLoanId DESC", ct);
    }

    public async Task<FarmerLoanDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var loan = await conn.QueryFirstOrNullAsync<FarmerLoanDto>(
            DetailSelect + " WHERE l.FarmerLoanId = @Id AND l.IsActive = 1", new { Id = id }, ct);
        if (loan is null) return null;

        loan.Installments = await conn.QueryListAsync<FarmerLoanInstallmentDto>(InstallmentSelect, new { LoanId = id }, ct);
        return loan;
    }

    public async Task<long> CreateAsync(CreateFarmerLoanRequest request, DateTime startDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var created = await conn.QueryCreatedSpAsync("sp_CreateFarmerLoan", new
        {
            p_FarmerId = request.FarmerId,
            p_Principal = request.PrincipalAmount,
            p_InterestRate = request.InterestRate,
            p_Installments = request.NumberOfInstallments,
            p_StartDate = startDate.Date,
            p_Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
            p_UserId = userId
        }, ct);
        return created.Id;
    }

    public async Task PayInstallmentAsync(long loanId, int installmentNo, decimal amount, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecuteSpAsync("sp_PayLoanInstallment", new
        {
            p_FarmerLoanId = loanId,
            p_InstallmentNo = installmentNo,
            p_Amount = amount,
            p_UserId = userId
        }, ct);
    }
}
