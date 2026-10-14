using Dairy.Application.Common;
using Dairy.Application.DTOs.Maintenance;
using Dairy.Application.Requests.Maintenance;

namespace Dairy.Application.Interfaces.Maintenance;

// ---------- equipment ----------

public interface IEquipmentRepository
{
    Task<PagedResult<EquipmentDto>> GetPagedAsync(EquipmentQuery query, CancellationToken ct = default);
    Task<EquipmentDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken ct = default);
    Task<bool> BranchExistsAsync(long id, CancellationToken ct = default);
    Task<bool> CollectionCenterExistsAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(CreateEquipmentRequest request, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateEquipmentRequest request, string status, bool isActive, long userId, CancellationToken ct = default);
}

public interface IEquipmentService
{
    Task<PagedResult<EquipmentDto>> GetPagedAsync(EquipmentQuery query, CancellationToken ct = default);
    Task<EquipmentDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request, CancellationToken ct = default);
    Task<EquipmentDto> UpdateAsync(long id, UpdateEquipmentRequest request, CancellationToken ct = default);
}

// ---------- schedules ----------

public interface IMaintenanceScheduleRepository
{
    Task<PagedResult<MaintenanceScheduleDto>> GetPagedAsync(MaintenanceScheduleQuery query, CancellationToken ct = default);
    Task<MaintenanceScheduleDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> EquipmentExistsAsync(long id, CancellationToken ct = default);
    Task<bool> EmployeeExistsAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(CreateMaintenanceScheduleRequest request, DateTime nextDueDate, long userId, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateMaintenanceScheduleRequest request, DateTime nextDueDate, string status, bool isActive, long userId, CancellationToken ct = default);
}

public interface IMaintenanceScheduleService
{
    Task<PagedResult<MaintenanceScheduleDto>> GetPagedAsync(MaintenanceScheduleQuery query, CancellationToken ct = default);
    Task<MaintenanceScheduleDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<MaintenanceScheduleDto> CreateAsync(CreateMaintenanceScheduleRequest request, CancellationToken ct = default);
    Task<MaintenanceScheduleDto> UpdateAsync(long id, UpdateMaintenanceScheduleRequest request, CancellationToken ct = default);
}

// ---------- records + due ----------

public interface IMaintenanceRecordRepository
{
    Task<PagedResult<MaintenanceRecordDto>> GetPagedAsync(MaintenanceRecordQuery query, CancellationToken ct = default);
    Task<MaintenanceRecordDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<MaintenanceRecordedDto> RecordAsync(CreateMaintenanceRecordRequest request, long userId, CancellationToken ct = default);
    Task SetExtrasAsync(long id, CreateMaintenanceRecordRequest request, CancellationToken ct = default);
    Task<List<MaintenanceDueDto>> GetDueAsync(int? days, CancellationToken ct = default);
}

public interface IMaintenanceRecordService
{
    Task<PagedResult<MaintenanceRecordDto>> GetPagedAsync(MaintenanceRecordQuery query, CancellationToken ct = default);
    Task<MaintenanceRecordDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<MaintenanceRecordedDto> CreateAsync(CreateMaintenanceRecordRequest request, CancellationToken ct = default);
    Task<List<MaintenanceDueDto>> GetDueAsync(int? days, CancellationToken ct = default);
}
