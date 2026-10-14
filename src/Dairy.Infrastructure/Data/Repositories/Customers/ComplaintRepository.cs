using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Customers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Customers;
using Dairy.Application.Requests.Customers;

namespace Dairy.Infrastructure.Data.Repositories.Customers;

public class ComplaintRepository : IComplaintRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "cc.CustomerComplaintId",
        ["complaintNumber"] = "cc.ComplaintNumber",
        ["customerName"] = "c.Name",
        ["category"] = "cc.Category",
        ["priority"] = "cc.Priority",
        ["status"] = "cc.Status",
        ["createdDate"] = "cc.CreatedDate"
    };

    private const string Select =
        "SELECT cc.CustomerComplaintId AS Id, cc.ComplaintNumber, cc.CustomerId, c.Name AS CustomerName, c.CustomerCode, cc.SalesOrderId, o.OrderNumber," +
        " cc.DeliveryId, d.DeliveryNumber, cc.Category, cc.Subject, cc.Description, cc.Priority, cc.Status, cc.AssignedTo, au.DisplayName AS AssignedToName," +
        " cc.AssignedDate, cc.Resolution, cc.ResolvedBy, cc.ResolvedDate, cc.ClosedBy, cc.ClosedDate, cc.FeedbackRating, cc.FeedbackComments, cc.FeedbackDate," +
        " cc.CreatedDate" +
        " FROM `CustomerComplaint` cc" +
        " JOIN `Customer` c ON c.CustomerId = cc.CustomerId" +
        " LEFT JOIN `SalesOrder` o ON o.SalesOrderId = cc.SalesOrderId" +
        " LEFT JOIN `Delivery` d ON d.DeliveryId = cc.DeliveryId" +
        " LEFT JOIN `User` au ON au.UserId = cc.AssignedTo";

    private const string CountFrom =
        " FROM `CustomerComplaint` cc JOIN `Customer` c ON c.CustomerId = cc.CustomerId";

    private const string Where =
        " WHERE cc.IsActive = 1" +
        " AND (@Search IS NULL OR cc.ComplaintNumber LIKE @Search OR cc.Subject LIKE @Search OR c.Name LIKE @Search OR c.CustomerCode LIKE @Search)" +
        " AND (@CustomerId IS NULL OR cc.CustomerId = @CustomerId)" +
        " AND (@Status IS NULL OR cc.Status = @Status)" +
        " AND (@Category IS NULL OR cc.Category = @Category)" +
        " AND (@Priority IS NULL OR cc.Priority = @Priority)" +
        " AND (@AssignedTo IS NULL OR cc.AssignedTo = @AssignedTo)" +
        " AND (@From IS NULL OR cc.CreatedDate >= @From)" +
        " AND (@To IS NULL OR cc.CreatedDate < DATE_ADD(@To, INTERVAL 1 DAY))";

    private readonly IDbConnectionFactory _db;

    public ComplaintRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ComplaintDto>> GetPagedAsync(ComplaintQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            query.CustomerId,
            Status = SalesValues.Clean(query.Status),
            Category = SalesValues.Clean(query.Category),
            Priority = SalesValues.Clean(query.Priority),
            query.AssignedTo,
            From = query.From?.Date,
            To = query.To?.Date
        };
        return await conn.QueryPagedAsync<ComplaintDto>(Select + Where, "SELECT COUNT(*)" + CountFrom + Where, param, query, SortColumns, "cc.CreatedDate DESC", ct);
    }

    public async Task<ComplaintDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<ComplaintDto>(Select + " WHERE cc.CustomerComplaintId = @Id AND cc.IsActive = 1", new { Id = id }, ct);
    }

    public async Task<bool> CustomerExistsAsync(long customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `Customer` WHERE CustomerId = @Id", new { Id = customerId }, ct) > 0;
    }

    public async Task<bool> OrderBelongsToCustomerAsync(long salesOrderId, long customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `SalesOrder` WHERE SalesOrderId = @Id AND CustomerId = @CustomerId",
            new { Id = salesOrderId, CustomerId = customerId }, ct) > 0;
    }

    public async Task<bool> DeliveryBelongsToCustomerAsync(long deliveryId, long customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Delivery` d JOIN `SalesOrder` o ON o.SalesOrderId = d.SalesOrderId WHERE d.DeliveryId = @Id AND o.CustomerId = @CustomerId",
            new { Id = deliveryId, CustomerId = customerId }, ct) > 0;
    }

    public async Task<bool> UserExistsAsync(long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.ScalarAsync<int>("SELECT COUNT(1) FROM `User` WHERE UserId = @Id AND IsActive = 1", new { Id = userId }, ct) > 0;
    }

    public async Task<CreatedResult> CreateAsync(long customerId, CreateComplaintRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        // number is derived from the new id in the same statement batch (LAST_INSERT_ID() is unchanged by the UPDATE)
        var id = await conn.InsertAsync(
            "INSERT INTO `CustomerComplaint` (ComplaintNumber, CustomerId, SalesOrderId, DeliveryId, Category, Subject, Description, Priority, Status, CreatedBy)" +
            " VALUES (CONCAT('TMP', LEFT(REPLACE(UUID(), '-', ''), 20)), @CustomerId, @SalesOrderId, @DeliveryId, @Category, @Subject, @Description, @Priority, 'Open', @UserId);" +
            " UPDATE `CustomerComplaint` SET ComplaintNumber = CONCAT('CMP', DATE_FORMAT(CURDATE(), '%y%m'), LPAD(LAST_INSERT_ID(), 6, '0'))" +
            " WHERE CustomerComplaintId = LAST_INSERT_ID()",
            new
            {
                CustomerId = customerId,
                r.SalesOrderId,
                r.DeliveryId,
                r.Category,
                Subject = r.Subject.Trim(),
                Description = r.Description.Trim(),
                r.Priority,
                UserId = userId
            }, ct);
        var number = await conn.ScalarAsync<string>("SELECT ComplaintNumber FROM `CustomerComplaint` WHERE CustomerComplaintId = @Id", new { Id = id }, ct);
        return new CreatedResult { Id = id, Number = number };
    }

    public async Task AssignAsync(long id, long assignedTo, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CustomerComplaint` SET AssignedTo = @AssignedTo, AssignedDate = NOW(), Status = IF(Status = 'Open', 'Assigned', Status), ModifiedBy = @UserId" +
            " WHERE CustomerComplaintId = @Id",
            new { Id = id, AssignedTo = assignedTo, UserId = userId }, ct);
    }

    public async Task SetStatusAsync(long id, string status, long? assignedTo, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CustomerComplaint` SET Status = @Status," +
            " AssignedTo = COALESCE(@AssignedTo, AssignedTo), AssignedDate = IF(@AssignedTo IS NULL, AssignedDate, NOW())," +
            " ResolvedBy = NULL, ResolvedDate = NULL, ModifiedBy = @UserId" +
            " WHERE CustomerComplaintId = @Id",
            new { Id = id, Status = status, AssignedTo = assignedTo, UserId = userId }, ct);
    }

    public async Task ResolveAsync(long id, string resolution, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CustomerComplaint` SET Status = 'Resolved', Resolution = @Resolution, ResolvedBy = @UserId, ResolvedDate = NOW(), ModifiedBy = @UserId" +
            " WHERE CustomerComplaintId = @Id",
            new { Id = id, Resolution = resolution, UserId = userId }, ct);
    }

    public async Task CloseAsync(long id, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CustomerComplaint` SET Status = 'Closed', ClosedBy = @UserId, ClosedDate = NOW(), ModifiedBy = @UserId WHERE CustomerComplaintId = @Id",
            new { Id = id, UserId = userId }, ct);
    }

    public async Task SaveFeedbackAsync(long id, int rating, string? comments, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CustomerComplaint` SET FeedbackRating = @Rating, FeedbackComments = @Comments, FeedbackDate = NOW(), ModifiedBy = @UserId" +
            " WHERE CustomerComplaintId = @Id",
            new { Id = id, Rating = rating, Comments = SalesValues.Clean(comments), UserId = userId }, ct);
    }

    public async Task AddNotificationAsync(long userId, string title, string message, string severity, string? link, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "INSERT INTO `Notification` (UserId, Title, Message, Severity, Link) VALUES (@UserId, @Title, @Message, @Severity, @Link)",
            new { UserId = userId, Title = title, Message = message, Severity = severity, Link = link }, ct);
    }
}
