using Dairy.Application.Common;
using Dairy.Application.DTOs.Admin;
using Dairy.Application.Interfaces;
using Dairy.Application.Interfaces.Admin;
using Dairy.Application.Requests.Admin;
using Dairy.Domain.Constants;

namespace Dairy.Application.Services.Admin;

public class ProfileService : IProfileService
{
    private readonly IProfileRepository _repo;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _user;

    public ProfileService(IProfileRepository repo, IAuditService audit, ICurrentUser user)
    {
        _repo = repo;
        _audit = audit;
        _user = user;
    }

    public async Task<ProfileDto> GetAsync(CancellationToken ct = default) =>
        await _repo.GetAsync(_user.UserId, ct) ?? throw new NotFoundException("User", _user.UserId);

    public async Task<ProfileDto> UpdateAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var existing = await GetAsync(ct);
        await _repo.UpdateAsync(_user.UserId, request.DisplayName.Trim(), B5Text.Clean(request.Email), B5Text.Clean(request.Mobile), ct);
        var updated = await GetAsync(ct);

        await _audit.LogAsync(AuditActions.Update, "User", _user.UserId,
            new { existing.DisplayName, existing.Email, existing.Mobile },
            new { updated.DisplayName, updated.Email, updated.Mobile }, ct);
        return updated;
    }
}
