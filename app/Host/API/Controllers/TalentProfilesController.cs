using System.Security.Claims;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.TalentProfiles.GetCurrent;
using Modules.TalentProfiles.UploadPicture;
using Modules.TalentProfiles.UploadResume;
using Modules.TalentProfiles.Upsert;
using Shared.Dtos.TalentProfiles;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Host.API.Controllers;

[ApiController]
[Route("api/v1/talent-profile")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class TalentProfilesController(
    IMediator mediator, 
    AppDbContext dbContext,
    IResumeStorageService resumeStorageService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IResumeStorageService _resumeStorageService = resumeStorageService;

    [HttpGet("me")]
    [ProducesResponseType(typeof(TalentProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        var response = await _mediator.Send(
            new GetCurrentTalentProfileQuery(userId.Value),
            cancellationToken);

        return Ok(response);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(TalentProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertCurrentAsync(
        [FromBody] UpsertTalentProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        var response = await _mediator.Send(
            new UpsertTalentProfileCommand(
                userId.Value,
                request.Name,
                request.Email,
                request.PhoneNumber,
                request.Location,
                request.Tagline,
                request.Bio,
                request.Linkedin,
                request.AdditionalUrl,
                request.VideoIntro,
                request.Experience,
                request.Workplaces,
                request.ExpectedSalary,
                request.Skills),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("me/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TalentProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadPictureAsync(
        [FromForm] UploadTalentProfilePictureRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        var response = await _mediator.Send(
            new UploadTalentProfilePictureCommand(
                userId.Value,
                request.Picture),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("me/resume")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TalentProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadResumeAsync(
        [FromForm] UploadTalentProfileResumeRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        var response = await _mediator.Send(
            new UploadTalentProfileResumeCommand(
                userId.Value,
                request.Resume),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("me/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadMyResumeAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        var profile = await _dbContext.TalentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId.Value, cancellationToken);

        if (profile is null || string.IsNullOrWhiteSpace(profile.ResumeUrl))
        {
            return NotFound("No resume found for this profile.");
        }

        var fileName = string.IsNullOrWhiteSpace(profile.ResumeFileName)
            ? "resume.pdf"
            : profile.ResumeFileName;

        var download = await _resumeStorageService.DownloadByUrlAsync(
            profile.ResumeUrl,
            cancellationToken);

        return File(download.Stream, download.ContentType, fileName);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private ObjectResult InvalidUserContextProblem()
    {
        return Problem(
            type: "https://httpstatuses.com/401",
            title: "Unauthorized",
            detail: "Invalid user context.",
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
