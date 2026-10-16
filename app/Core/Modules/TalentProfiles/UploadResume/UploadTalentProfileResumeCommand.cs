using MediatR;
using Microsoft.AspNetCore.Http;
using Shared.Dtos.TalentProfiles;

namespace Modules.TalentProfiles.UploadResume;

public sealed record UploadTalentProfileResumeCommand(
    Guid UserId,
    IFormFile Resume) : IRequest<TalentProfileDto>;
