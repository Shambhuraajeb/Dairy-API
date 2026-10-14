using Dairy.Api.Authorization;
using Dairy.Application.Interfaces.Customers;
using Dairy.Application.Requests.Customers;
using Dairy.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Dairy.Api.Controllers.Customers;

[Route("api/customer-complaints")]
public class CustomerComplaintsController : ApiControllerBase
{
    private readonly IComplaintService _complaints;

    public CustomerComplaintsController(IComplaintService complaints) => _complaints = complaints;

    [HttpGet]
    [HasPermission(Permissions.ComplaintView)]
    public async Task<IActionResult> List([FromQuery] ComplaintQuery query, CancellationToken ct) =>
        Paged(await _complaints.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.ComplaintView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct) =>
        Ok(await _complaints.GetByIdAsync(id, ct));

    /// <summary>Customers raise complaints for themselves (customerId ignored); staff pass customerId.</summary>
    [HttpPost]
    [HasPermission(Permissions.ComplaintCreate)]
    public async Task<IActionResult> Create([FromBody] CreateComplaintRequest request, CancellationToken ct) =>
        Created(await _complaints.CreateAsync(request, ct), "Complaint registered.");

    [HttpPost("{id:long}/assign")]
    [HasPermission(Permissions.ComplaintUpdate)]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignComplaintRequest request, CancellationToken ct) =>
        Ok(await _complaints.AssignAsync(id, request, ct), "Complaint assigned.");

    [HttpPost("{id:long}/status")]
    [HasPermission(Permissions.ComplaintUpdate)]
    public async Task<IActionResult> SetStatus(long id, [FromBody] ComplaintStatusRequest request, CancellationToken ct) =>
        Ok(await _complaints.SetStatusAsync(id, request, ct), "Complaint status updated.");

    [HttpPost("{id:long}/resolve")]
    [HasPermission(Permissions.ComplaintUpdate)]
    public async Task<IActionResult> Resolve(long id, [FromBody] ResolveComplaintRequest request, CancellationToken ct) =>
        Ok(await _complaints.ResolveAsync(id, request, ct), "Complaint resolved.");

    [HttpPost("{id:long}/close")]
    [HasPermission(Permissions.ComplaintUpdate)]
    public async Task<IActionResult> Close(long id, CancellationToken ct) =>
        Ok(await _complaints.CloseAsync(id, ct), "Complaint closed.");

    /// <summary>Only the customer who raised the complaint (checked in the service).</summary>
    [HttpPost("{id:long}/feedback")]
    [HasPermission(Permissions.ComplaintView)]
    public async Task<IActionResult> Feedback(long id, [FromBody] ComplaintFeedbackRequest request, CancellationToken ct) =>
        Ok(await _complaints.FeedbackAsync(id, request, ct), "Thank you for your feedback.");
}
