# Backend patterns (read first). Reference module: `Units` (grep "Unit" across src/)

Projects in `backend/src/`: Domain -> Application -> Infrastructure -> Api. NEVER edit Program.cs / ServiceCollectionExtensions.cs:
`*Service` (Application, interface `I<Name>`) and `*Repository` (Infrastructure, interface `I<Name>`) are auto-registered scoped;
validators auto-registered. Interface name must be exactly `I` + class name. Don't name helper classes `*Service`/`*Repository` unless they are one.

## Files to add per module `Xyz` (namespaces get a `.Xyz` suffix; e.g. `Dairy.Application.Services.Xyz`)
- `Application/DTOs/Xyz/XyzDto.cs` (plain classes, settable props, PK exposed as `Id`, plus joined display names)
- `Application/Requests/Xyz/*.cs` (`XyzQuery : PagedQuery` for list filters, `CreateXyzRequest`, `UpdateXyzRequest`)
- `Application/Validators/Xyz/*Validator.cs` (`AbstractValidator<T>`; password rule: `.MustBeStrongPassword()`)
- `Application/Interfaces/Xyz/{IXyzService,IXyzRepository}.cs`, `Application/Services/Xyz/XyzService.cs`
- `Infrastructure/Data/Repositories/Xyz/XyzRepository.cs`, `Api/Controllers/Xyz/XyzController.cs` (or flat in Controllers/)
- Domain constants already exist: `Permissions.*`, `Roles.*`, statuses (`MilkCollectionStatus`, `PaymentStatus`, `OrderStatus`...), `AuditActions.*`.

## Controller (thin; one service call; `ApiControllerBase` gives `Ok<T>(data,msg)`, `Created<T>(data,msg)`, `Paged<T>(PagedResult<T>)`, `Message(msg)`)
```csharp
[Route("api/inventory/units")]
public class UnitsController : ApiControllerBase {
  [HttpGet][HasPermission(Permissions.InventoryView)]
  public async Task<IActionResult> List([FromQuery] UnitQuery q, CancellationToken ct) => Paged(await _units.GetPagedAsync(q, ct));
  [HttpPost][HasPermission(Permissions.InventoryCreate)]
  public async Task<IActionResult> Create([FromBody] CreateUnitRequest r, CancellationToken ct) => Created(await _units.CreateAsync(r, ct), "Unit created.");
}
```
`[HasPermission("a","b")]` = any of; stack attributes = AND. Request bodies are auto-validated (400 + field errors) if an `IValidator<T>` exists.
Status actions: `[HttpPost("{id:long}/approve")]` returning `Message("Approved.")` or `Ok(dto)`.

## Service: business rules + audit + ownership. Throw, never return error codes
`NotFoundException(entity,id)` 404, `BusinessRuleException(msg)` 422, `ConflictException(msg)` 409, `ForbiddenException()` 403,
`RequestValidationException(field,msg)` 400. Inject `ICurrentUser _user` (`UserId` long, `Role`, `FarmerId?`, `CustomerId?`, `EmployeeId?`, `HasPermission(code)`, `IsInRole`) and `IAuditService _audit`:
`await _audit.LogAsync(AuditActions.Update, "Unit", id, oldDto, newDto, ct);` (never throws; call AFTER the DB change succeeds).
Ownership: in the service, `if (_user.IsInRole(Roles.Farmer)) query.FarmerId = _user.FarmerId ?? throw new ForbiddenException();`
for single records load it then `if (_user.IsInRole(Roles.Farmer) && dto.FarmerId != _user.FarmerId) throw new NotFoundException("Farmer", id);`
Customers: `Roles.IsCustomerRole(_user.Role)` -> `_user.CustomerId`. Driver: filter by `_user.EmployeeId`.

## Repository (Infrastructure; `using Dairy.Infrastructure.Data;` gives the extension methods on the connection)
```csharp
await using var conn = await _db.OpenAsync(ct);                       // IDbConnectionFactory _db
var p = new { Search = q.SearchPattern, q.IsActive };                  // SearchPattern = "%text%" (escaped) or null
return await conn.QueryPagedAsync<UnitDto>(
   "SELECT u.UnitId AS Id, u.Code, u.Name, u.IsActive FROM `Unit` u WHERE (@Search IS NULL OR u.Name LIKE @Search)",
   "SELECT COUNT(*) FROM `Unit` u WHERE (@Search IS NULL OR u.Name LIKE @Search)",   // same FROM/WHERE, same params
   p, q, SortColumns /*Dictionary: camelCase key -> trusted column*/, "u.Name ASC", ct);   // no ORDER BY/LIMIT in selectSql
```
Signatures (all `this IDbConnection conn`, all end with `CancellationToken ct = default`):
- `Task<PagedResult<T>> QueryPagedAsync<T>(string selectSql, string countSql, object? param, PagedQuery query, IReadOnlyDictionary<string,string> sortWhitelist, string defaultSort, ct)`
- `Task<List<T>> QueryListAsync<T>(string sql, object? param = null, ct)`; `Task<T?> QueryFirstOrNullAsync<T>(sql, param, ct)`; `Task<T?> ScalarAsync<T>(sql, param, ct)`
- `Task<int> ExecAsync(sql, param, ct)`; `Task<long> InsertAsync(insertSql, param, ct)` (returns LAST_INSERT_ID)
- Stored procs (param names are `p_<Name>`): `QuerySpAsync<T>(sp, new { p_FarmerId = id }, ct)` -> `List<T>`; `QuerySpFirstOrDefaultAsync<T>`;
  `QueryMultipleSpAsync(sp, param, ct)` -> `SqlMapper.GridReader` (`using var g = ...; await g.ReadAsync<A>(); await g.ReadFirstOrDefaultAsync<B>()`);
  `ExecuteSpAsync(sp, param, ct)`; `ExecuteSpScalarAsync<T>`; `QueryCreatedSpAsync(sp, param, ct)` -> `CreatedResult {long Id; string? Number}` for creating SPs.
SP business-rule `SIGNAL SQLSTATE '45000'` is automatically returned as HTTP 422 with its message; 1062 -> 409; 1451/1452 -> 409/400. No try/catch needed.
Rules: always backtick table names (`` `User` ``, `` `Order` ``); alias PK `XxxId AS Id`; never concatenate input into SQL (sort via whitelist only);
use `DateTime`/`DateTime?` (also `DateOnly`/`TimeOnly` work); bool for TINYINT(1); `decimal` for DECIMAL; `long` ids. DB time is server-local (`NOW()`), so compare with `DateTime.Now`.
Set `CreatedBy`/`ModifiedBy` from `_user.UserId` (pass into repository). Soft-deactivate with `IsActive` instead of deleting.

## Other infrastructure
- `PagedQuery`: `Page`, `PageSize` (max 200), `Search`, `SortBy`, `SortDir`, `Offset`, `SearchPattern`, `Descending`. `PagedResult<T>`: `Items, Total, Page, PageSize, TotalPages`.
- `IFileStorage.SaveAsync(stream, fileName, "farmers/12", ct)` -> `StoredFile.RelativePath` (store it in the DB); `OpenRead(rel)` -> `Stream?`; `Delete(rel)`.
- `IPasswordHasher.Hash/Verify`; `PasswordPolicy.Validate(pw)` (in `Dairy.Application.BusinessRules`).
- JSON is camelCase with string enums; responses are `ApiResponse<T> {success,message,data,errors[]}`; keep DTO property names = SQL aliases.
- Document your endpoints in `backend/docs/api/<group>.md` (see foundation.md there for format).
