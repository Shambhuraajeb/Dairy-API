using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Hr;
using Dairy.Application.Requests.Hr;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dairy.Api.Controllers.Hr;

[Route("api/designations")]
public class DesignationsController : ApiControllerBase
{
    private readonly IDesignationService _designations;

    public DesignationsController(IDesignationService designations) => _designations = designations;

    [HttpGet]
    [HasPermission(Permissions.EmployeeView)]
    public async Task<IActionResult> List([FromQuery] DesignationQuery query, CancellationToken ct) =>
        Paged(await _designations.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.EmployeeView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _designations.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.EmployeeCreate)]
    public async Task<IActionResult> Create([FromBody] CreateDesignationRequest request, CancellationToken ct) =>
        Created(await _designations.CreateAsync(request, ct), "Designation created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.EmployeeUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDesignationRequest request, CancellationToken ct) =>
        Ok(await _designations.UpdateAsync(id, request, ct), "Designation updated.");

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.EmployeeUpdate)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _designations.DeactivateAsync(id, ct);
        return Message("Designation deactivated.");
    }
}

[Route("api/employees")]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employees;

    public EmployeesController(IEmployeeService employees) => _employees = employees;

    [HttpGet]
    [HasPermission(Permissions.EmployeeView)]
    public async Task<IActionResult> List([FromQuery] EmployeeQuery query, CancellationToken ct) =>
        Paged(await _employees.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.EmployeeView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _employees.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.EmployeeCreate)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct) =>
        Created(await _employees.CreateAsync(request, ct), "Employee created.");

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.EmployeeUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct) =>
        Ok(await _employees.UpdateAsync(id, request, ct), "Employee updated.");

    [HttpPost("{id:long}/deactivate")]
    [HasPermission(Permissions.EmployeeUpdate)]
    public async Task<IActionResult> Deactivate(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeactivateEmployeeRequest? request, CancellationToken ct) =>
        Ok(await _employees.DeactivateAsync(id, request, ct), "Employee deactivated.");

    [HttpPost("{id:long}/activate")]
    [HasPermission(Permissions.EmployeeUpdate)]
    public async Task<IActionResult> Activate(long id, CancellationToken ct) =>
        Ok(await _employees.ActivateAsync(id, ct), "Employee activated.");
}

[Route("api/attendance")]
public class AttendanceController : ApiControllerBase
{
    private readonly IAttendanceService _attendance;

    public AttendanceController(IAttendanceService attendance) => _attendance = attendance;

    [HttpGet]
    [HasPermission(Permissions.AttendanceView)]
    public async Task<IActionResult> List([FromQuery] AttendanceQuery query, CancellationToken ct) =>
        Paged(await _attendance.GetPagedAsync(query, ct));

    /// <summary>All active employees with their attendance for one day (default today); for the marking screen.</summary>
    [HttpGet("sheet")]
    [HasPermission(Permissions.AttendanceView)]
    public async Task<IActionResult> Sheet([FromQuery] AttendanceSheetQuery query, CancellationToken ct) =>
        Ok(await _attendance.GetSheetAsync(query, ct));

    [HttpPost]
    [HasPermission(Permissions.AttendanceManage)]
    public async Task<IActionResult> Upsert([FromBody] UpsertAttendanceRequest request, CancellationToken ct) =>
        Ok(await _attendance.UpsertAsync(request, ct), "Attendance saved.");

    [HttpPost("bulk")]
    [HasPermission(Permissions.AttendanceManage)]
    public async Task<IActionResult> Bulk([FromBody] BulkAttendanceRequest request, CancellationToken ct) =>
        Ok(await _attendance.BulkUpsertAsync(request, ct), "Attendance saved.");
}

[Route("api/leave-requests")]
public class LeaveRequestsController : ApiControllerBase
{
    private readonly ILeaveRequestService _leaves;

    public LeaveRequestsController(ILeaveRequestService leaves) => _leaves = leaves;

    [HttpGet]
    [HasPermission(Permissions.LeaveView)]
    public async Task<IActionResult> List([FromQuery] LeaveRequestQuery query, CancellationToken ct) =>
        Paged(await _leaves.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.LeaveView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _leaves.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.LeaveCreate)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequest request, CancellationToken ct) =>
        Created(await _leaves.CreateAsync(request, ct), "Leave request submitted.");

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.LeaveApprove)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct) =>
        Ok(await _leaves.ApproveAsync(id, ct), "Leave approved.");

    [HttpPost("{id:long}/reject")]
    [HasPermission(Permissions.LeaveApprove)]
    public async Task<IActionResult> Reject(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RejectLeaveRequest? request, CancellationToken ct) =>
        Ok(await _leaves.RejectAsync(id, request, ct), "Leave rejected.");
}

[Route("api/salaries")]
public class SalariesController : ApiControllerBase
{
    private readonly ISalaryService _salaries;

    public SalariesController(ISalaryService salaries) => _salaries = salaries;

    [HttpGet]
    [HasPermission(Permissions.SalaryView)]
    public async Task<IActionResult> List([FromQuery] SalaryQuery query, CancellationToken ct) =>
        Paged(await _salaries.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.SalaryView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _salaries.GetByIdAsync(id, ct));

    [HttpPost("generate")]
    [HasPermission(Permissions.SalaryManage)]
    public async Task<IActionResult> Generate([FromBody] GenerateSalariesRequest request, CancellationToken ct) =>
        Ok(await _salaries.GenerateAsync(request, ct), "Salaries generated.");

    [HttpPost("{id:long}/pay")]
    [HasPermission(Permissions.SalaryManage)]
    public async Task<IActionResult> Pay(long id, [FromBody] PaySalaryRequest request, CancellationToken ct) =>
        Ok(await _salaries.PayAsync(id, request, ct), "Salary paid.");
}
