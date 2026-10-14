using Dairy.Application.BusinessRules;
using Dairy.Application.Common;
using Dairy.Application.DTOs.Customers;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Customers;
using Dairy.Application.Requests.Customers;

namespace Dairy.Infrastructure.Data.Repositories.Customers;

public class CustomerRepository : ICustomerRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "c.CustomerId",
        ["customerCode"] = "c.CustomerCode",
        ["name"] = "c.Name",
        ["customerType"] = "c.CustomerType",
        ["mobile"] = "c.Mobile",
        ["status"] = "c.Status",
        ["outstandingBalance"] = "c.OutstandingBalance",
        ["joinDate"] = "c.JoinDate",
        ["createdDate"] = "c.CreatedDate"
    };

    private const string Select =
        "SELECT c.CustomerId AS Id, c.CustomerCode, c.CustomerType, c.Name, c.ContactPerson, c.Mobile, c.AlternateMobile, c.Email, c.GstNumber," +
        " c.BranchId, b.Name AS BranchName, c.CreditLimit, c.CreditDays, c.DiscountPercent, c.OutstandingBalance, c.JoinDate, c.Status, c.Notes," +
        " c.IsActive, c.CreatedDate," +
        " (SELECT COUNT(*) FROM `CustomerSubscription` s WHERE s.CustomerId = c.CustomerId AND s.Status IN ('Active', 'Paused') AND s.IsActive = 1) AS ActiveSubscriptions" +
        " FROM `Customer` c LEFT JOIN `Branch` b ON b.BranchId = c.BranchId";

    private const string From = " FROM `Customer` c";

    private const string Where =
        " WHERE (@Search IS NULL OR c.Name LIKE @Search OR c.CustomerCode LIKE @Search OR c.Mobile LIKE @Search OR c.Email LIKE @Search OR c.ContactPerson LIKE @Search)" +
        " AND (@Type IS NULL OR c.CustomerType = @Type)" +
        " AND (@Status IS NULL OR c.Status = @Status)" +
        " AND (@IsActive IS NULL OR c.IsActive = @IsActive)" +
        " AND (@CustomerId IS NULL OR c.CustomerId = @CustomerId)";

    private const string AddressSelect =
        "SELECT a.CustomerAddressId AS Id, a.CustomerId, a.AddressType, a.ContactName, a.ContactMobile, a.Line1, a.Line2, a.Landmark, a.City, a.State, a.Pincode," +
        " a.Latitude, a.Longitude, a.RouteId, r.Name AS RouteName, a.DeliveryInstructions, a.IsDefault, a.IsActive" +
        " FROM `CustomerAddress` a LEFT JOIN `Route` r ON r.RouteId = a.RouteId";

    private readonly IDbConnectionFactory _db;

    public CustomerRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(CustomerQuery query, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var param = new
        {
            Search = query.SearchPattern,
            Type = SalesValues.Clean(query.Type),
            Status = SalesValues.Clean(query.Status),
            query.IsActive,
            query.CustomerId
        };
        return await conn.QueryPagedAsync<CustomerDto>(Select + Where, "SELECT COUNT(*)" + From + Where, param, query, SortColumns, "c.Name ASC", ct);
    }

    public async Task<CustomerDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<CustomerDto>(Select + " WHERE c.CustomerId = @Id", new { Id = id }, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `Customer` WHERE CustomerCode = @Code AND (@ExcludeId IS NULL OR CustomerId <> @ExcludeId)",
            new { Code = code, ExcludeId = excludeId }, ct);
        return count > 0;
    }

    public async Task<CreatedResult> CreateAsync(string? code, CreateCustomerRequest r, DateTime joinDate, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var (id, finalCode) = await AutoCode.InsertAsync(conn, "Customer", "CustomerCode", "CUS", 4, code, c => conn.InsertAsync(
            "INSERT INTO `Customer` (CustomerCode, CustomerType, Name, ContactPerson, Mobile, AlternateMobile, Email, GstNumber, BranchId, CreditLimit, CreditDays," +
            " DiscountPercent, JoinDate, Status, Notes, CreatedBy)" +
            " VALUES (@Code, @CustomerType, @Name, @ContactPerson, @Mobile, @AlternateMobile, @Email, @GstNumber, @BranchId, @CreditLimit, @CreditDays," +
            " @DiscountPercent, @JoinDate, 'Active', @Notes, @UserId)",
            new
            {
                Code = c,
                r.CustomerType,
                Name = r.Name.Trim(),
                ContactPerson = SalesValues.Clean(r.ContactPerson),
                Mobile = r.Mobile.Trim(),
                AlternateMobile = SalesValues.Clean(r.AlternateMobile),
                Email = SalesValues.Clean(r.Email),
                GstNumber = SalesValues.Clean(r.GstNumber),
                r.BranchId,
                r.CreditLimit,
                r.CreditDays,
                r.DiscountPercent,
                JoinDate = joinDate.Date,
                Notes = SalesValues.Clean(r.Notes),
                UserId = userId
            }, ct), ct);
        return new CreatedResult { Id = id, Number = finalCode };
    }

    public async Task UpdateAsync(long id, UpdateCustomerRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Customer` SET CustomerType = @CustomerType, Name = @Name, ContactPerson = @ContactPerson, Mobile = @Mobile, AlternateMobile = @AlternateMobile," +
            " Email = @Email, GstNumber = @GstNumber, BranchId = @BranchId, CreditLimit = @CreditLimit, CreditDays = @CreditDays, DiscountPercent = @DiscountPercent," +
            " JoinDate = COALESCE(@JoinDate, JoinDate), Notes = @Notes, ModifiedBy = @UserId WHERE CustomerId = @Id",
            new
            {
                Id = id,
                r.CustomerType,
                Name = r.Name.Trim(),
                ContactPerson = SalesValues.Clean(r.ContactPerson),
                Mobile = r.Mobile.Trim(),
                AlternateMobile = SalesValues.Clean(r.AlternateMobile),
                Email = SalesValues.Clean(r.Email),
                GstNumber = SalesValues.Clean(r.GstNumber),
                r.BranchId,
                r.CreditLimit,
                r.CreditDays,
                r.DiscountPercent,
                JoinDate = r.JoinDate?.Date,
                Notes = SalesValues.Clean(r.Notes),
                UserId = userId
            }, ct);
    }

    public async Task SetStatusAsync(long id, string status, bool isActive, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `Customer` SET Status = @Status, IsActive = @IsActive, ModifiedBy = @UserId WHERE CustomerId = @Id",
            new { Id = id, Status = status, IsActive = isActive, UserId = userId }, ct);
    }

    public async Task<CustomerSummaryDto?> GetSummaryAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<CustomerSummaryDto>(
            "SELECT c.CustomerId AS Id," +
            " (SELECT COUNT(*) FROM `CustomerSubscription` s WHERE s.CustomerId = c.CustomerId AND s.Status = 'Active' AND s.IsActive = 1) AS ActiveSubscriptions," +
            " (SELECT COUNT(*) FROM `CustomerSubscription` s WHERE s.CustomerId = c.CustomerId AND s.Status = 'Paused' AND s.IsActive = 1) AS PausedSubscriptions," +
            " (SELECT COUNT(*) FROM `SalesOrder` o WHERE o.CustomerId = c.CustomerId AND o.IsActive = 1 AND o.Status <> 'Cancelled') AS TotalOrders," +
            " (SELECT COUNT(*) FROM `SalesOrder` o WHERE o.CustomerId = c.CustomerId AND o.IsActive = 1 AND o.Status IN ('Pending', 'Confirmed', 'Scheduled', 'Dispatched')) AS PendingOrders," +
            " (SELECT COUNT(*) FROM `SalesOrder` o WHERE o.CustomerId = c.CustomerId AND o.IsActive = 1 AND o.Status = 'Delivered') AS DeliveredOrders," +
            " (SELECT MAX(o.OrderDate) FROM `SalesOrder` o WHERE o.CustomerId = c.CustomerId AND o.IsActive = 1 AND o.Status <> 'Cancelled') AS LastOrderDate," +
            " (SELECT COALESCE(SUM(i.TotalAmount), 0) FROM `Invoice` i WHERE i.CustomerId = c.CustomerId AND i.IsActive = 1 AND i.Status <> 'Cancelled') AS TotalInvoiced," +
            " (SELECT COUNT(*) FROM `Invoice` i WHERE i.CustomerId = c.CustomerId AND i.IsActive = 1 AND i.Status IN ('Issued', 'PartiallyPaid') AND i.BalanceAmount > 0) AS OpenInvoices," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE i.CustomerId = c.CustomerId AND i.IsActive = 1 AND i.Status IN ('Issued', 'PartiallyPaid')) AS OutstandingAmount," +
            " (SELECT COALESCE(SUM(i.BalanceAmount), 0) FROM `Invoice` i WHERE i.CustomerId = c.CustomerId AND i.IsActive = 1 AND i.Status IN ('Issued', 'PartiallyPaid') AND i.DueDate < CURDATE()) AS OverdueAmount," +
            " (SELECT COALESCE(SUM(p.UnallocatedAmount), 0) FROM `CustomerPayment` p WHERE p.CustomerId = c.CustomerId AND p.Status = 'Received') AS UnallocatedCredit," +
            " (SELECT p.PaymentDate FROM `CustomerPayment` p WHERE p.CustomerId = c.CustomerId AND p.Status = 'Received' ORDER BY p.PaymentDate DESC, p.CustomerPaymentId DESC LIMIT 1) AS LastPaymentDate," +
            " (SELECT p.Amount FROM `CustomerPayment` p WHERE p.CustomerId = c.CustomerId AND p.Status = 'Received' ORDER BY p.PaymentDate DESC, p.CustomerPaymentId DESC LIMIT 1) AS LastPaymentAmount," +
            " (SELECT COUNT(*) FROM `CustomerComplaint` cc WHERE cc.CustomerId = c.CustomerId AND cc.IsActive = 1 AND cc.Status IN ('Open', 'Assigned', 'InProgress')) AS OpenComplaints" +
            " FROM `Customer` c WHERE c.CustomerId = @Id",
            new { Id = id }, ct);
    }

    // ---------- addresses

    public async Task<List<CustomerAddressDto>> GetAddressesAsync(long customerId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryListAsync<CustomerAddressDto>(
            AddressSelect + " WHERE a.CustomerId = @CustomerId AND a.IsActive = 1 ORDER BY a.IsDefault DESC, a.CustomerAddressId",
            new { CustomerId = customerId }, ct);
    }

    public async Task<CustomerAddressDto?> GetAddressAsync(long customerId, long addressId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        return await conn.QueryFirstOrNullAsync<CustomerAddressDto>(
            AddressSelect + " WHERE a.CustomerAddressId = @AddressId AND a.CustomerId = @CustomerId AND a.IsActive = 1",
            new { AddressId = addressId, CustomerId = customerId }, ct);
    }

    public async Task<long> CreateAddressAsync(long customerId, CustomerAddressRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var existing = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CustomerAddress` WHERE CustomerId = @CustomerId AND IsActive = 1", new { CustomerId = customerId }, ct);
        var makeDefault = r.IsDefault || existing == 0;

        var id = await conn.InsertAsync(
            "INSERT INTO `CustomerAddress` (CustomerId, AddressType, ContactName, ContactMobile, Line1, Line2, Landmark, City, State, Pincode, Latitude, Longitude," +
            " RouteId, DeliveryInstructions, IsDefault, CreatedBy)" +
            " VALUES (@CustomerId, @AddressType, @ContactName, @ContactMobile, @Line1, @Line2, @Landmark, @City, @State, @Pincode, @Latitude, @Longitude," +
            " @RouteId, @DeliveryInstructions, @IsDefault, @UserId)",
            AddressParams(customerId, null, r, makeDefault, userId), ct);

        if (makeDefault)
            await UnsetOtherDefaultsAsync(conn, customerId, id, ct);
        return id;
    }

    public async Task UpdateAddressAsync(long customerId, long addressId, CustomerAddressRequest r, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CustomerAddress` SET AddressType = @AddressType, ContactName = @ContactName, ContactMobile = @ContactMobile, Line1 = @Line1, Line2 = @Line2," +
            " Landmark = @Landmark, City = @City, State = @State, Pincode = @Pincode, Latitude = @Latitude, Longitude = @Longitude, RouteId = @RouteId," +
            " DeliveryInstructions = @DeliveryInstructions, IsDefault = (IsDefault OR @IsDefault), ModifiedBy = @UserId" +
            " WHERE CustomerAddressId = @AddressId AND CustomerId = @CustomerId",
            AddressParams(customerId, addressId, r, r.IsDefault, userId), ct);

        if (r.IsDefault)
            await UnsetOtherDefaultsAsync(conn, customerId, addressId, ct);
    }

    public async Task<bool> AddressInUseAsync(long addressId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        var count = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CustomerSubscription` WHERE CustomerAddressId = @AddressId AND IsActive = 1 AND Status IN ('Active', 'Paused')",
            new { AddressId = addressId }, ct);
        return count > 0;
    }

    public async Task DeactivateAddressAsync(long customerId, long addressId, long userId, CancellationToken ct = default)
    {
        await using var conn = await _db.OpenAsync(ct);
        await conn.ExecAsync(
            "UPDATE `CustomerAddress` SET IsActive = 0, IsDefault = 0, ModifiedBy = @UserId WHERE CustomerAddressId = @AddressId AND CustomerId = @CustomerId",
            new { AddressId = addressId, CustomerId = customerId, UserId = userId }, ct);

        // keep one default address when any active address remains
        var hasDefault = await conn.ScalarAsync<int>(
            "SELECT COUNT(1) FROM `CustomerAddress` WHERE CustomerId = @CustomerId AND IsActive = 1 AND IsDefault = 1",
            new { CustomerId = customerId }, ct);
        if (hasDefault > 0) return;

        var firstId = await conn.ScalarAsync<long?>(
            "SELECT MIN(CustomerAddressId) FROM `CustomerAddress` WHERE CustomerId = @CustomerId AND IsActive = 1",
            new { CustomerId = customerId }, ct);
        if (firstId.HasValue)
            await conn.ExecAsync("UPDATE `CustomerAddress` SET IsDefault = 1 WHERE CustomerAddressId = @Id", new { Id = firstId.Value }, ct);
    }

    private static object AddressParams(long customerId, long? addressId, CustomerAddressRequest r, bool isDefault, long userId) => new
    {
        CustomerId = customerId,
        AddressId = addressId,
        r.AddressType,
        ContactName = SalesValues.Clean(r.ContactName),
        ContactMobile = SalesValues.Clean(r.ContactMobile),
        Line1 = r.Line1.Trim(),
        Line2 = SalesValues.Clean(r.Line2),
        Landmark = SalesValues.Clean(r.Landmark),
        City = SalesValues.Clean(r.City),
        State = SalesValues.Clean(r.State),
        Pincode = SalesValues.Clean(r.Pincode),
        r.Latitude,
        r.Longitude,
        r.RouteId,
        DeliveryInstructions = SalesValues.Clean(r.DeliveryInstructions),
        IsDefault = isDefault,
        UserId = userId
    };

    private static Task<int> UnsetOtherDefaultsAsync(System.Data.IDbConnection conn, long customerId, long keepAddressId, CancellationToken ct) =>
        conn.ExecAsync(
            "UPDATE `CustomerAddress` SET IsDefault = 0 WHERE CustomerId = @CustomerId AND CustomerAddressId <> @KeepId AND IsDefault = 1",
            new { CustomerId = customerId, KeepId = keepAddressId }, ct);
}
