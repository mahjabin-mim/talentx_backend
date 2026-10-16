using MediatR;
using Microsoft.AspNetCore.Http;
using Shared.Dtos.TalentProfiles;

namespace Modules.TalentProfiles.UploadPicture;

public sealed record UploadTalentProfilePictureCommand(
    Guid UserId,
    IFormFile Picture) : IRequest<TalentProfileDto>;
