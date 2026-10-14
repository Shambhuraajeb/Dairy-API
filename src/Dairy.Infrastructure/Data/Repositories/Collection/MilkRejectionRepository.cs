using Dairy.Application.Common;
using Dairy.Application.DTOs.Collection;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Collection;
using Dairy.Application.Requests.Collection;

namespace Dairy.Infrastructure.Data.Repositories.Collection;

public class MilkRejectionRepository : IMilkRejectionRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "r.MilkRejectionId",
        ["rejectionDate"] = "r.RejectionDate",
        ["collectionNumber"] = "mc.CollectionNumber",
        ["farmerName"] = "f.FullName",
        ["collectionCenterName"] = "cc.Name",
        ["reasonCode"] = "r.ReasonCode",
        ["quantityLiters"] = "r.QuantityLiters",
        ["rejectedDate"] = "r.RejectedDate"
    };

    private const string Select =
        "SELECT r.MilkRejectionId AS Id, r.MilkCollectionId, mc.CollectionNumber, mc.CollectionDate, r.FarmerId, f.FarmerCode, " +
        "f.FullName AS FarmerName, r.CollectionCenterId, cc.Name AS CollectionCenterName, r.RejectionDate, r.ReasonCode, r.Reason, " +
        "r.QuantityLiters, r.DisposalMethod, r.RejectedBy, u.DisplayName AS RejectedByName, r.RejectedDate, r.Remarks";

    private const string FromJoins =
        " FROM `MilkRejection` r" +
        " JOIN `MilkCollection` mc ON mc.MilkCollectionId = r.MilkCollectionId" +
        " JOIN `Farmer` f ON f.FarmerId = r.FarmerId" +
        " LEFT JOIN `CollectionCenter` cc ON cc.CollectionCenterId = r.CollectionCenterId" +
        " LEFT JOIN `User` u ON u.UserId = r.RejectedBy";

    private const string Where =
        " WHERE r.IsActive = 1" +
        " AND (@DateFrom IS NULL OR r.RejectionDate >= @DateFrom)" +
        " AND (@DateTo IS NULL OR r.RejectionDate <= @DateTo)" +
        " AND (@CenterId IS NULL OR r.CollectionCenterId = @CenterId)" +
        " AND (@FarmerId IS NULL OR r.FarmerId = @FarmerId)" +
        " AND (@ReasonCode IS NULL OR r.ReasonCode = @ReasonCode)" +
        " AND (@Search IS NULL OR mc.CollectionNumber LIKE @Search OR f.FullName LIKE @Search OR f.FarmerCode LIKE @Search OR r.Reason LIKE @Search)";

    private readonly IDbConnectionFactory _db;

    public MilkRejectionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<MilkRejectionDto>> GetPagedAsync(MilkRejectionQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            DateFrom = query.From?.Date,
            DateTo = query.To?.Date,
            CenterId = query.CenterId,
            FarmerId = query.FarmerId,
            ReasonCode = string.IsNullOrWhiteSpace(query.ReasonCode) ? null : query.ReasonCode.Trim(),
            Search = query.SearchPattern
        };
        return await conn.QueryPagedAsync<MilkRejectionDto>(
            Select + FromJoins + Where,
            "SELECT COUNT(*)" + FromJoins + Where,
            param, query, SortColumns, "r.RejectedDate DESC, r.MilkRejectionId DESC", ct);
    }
}
