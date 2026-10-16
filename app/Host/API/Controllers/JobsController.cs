using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Jobs.CreateJob;
using Modules.Jobs.DeleteJob;
using Modules.Jobs.GenerateJobDescription;
using Modules.Jobs.GetEmployerDashboard;
using Modules.Jobs.GetJobById;
using Modules.Jobs.GetJobApplicants;
using Modules.Jobs.GetPublicJobs;
using Modules.Jobs.UpdateJob;
using Modules.Jobs.UpdateJobStatus;
using Modules.Jobs.UpdateApplicationStatus;
using Shared.Dtos.Jobs;
using Shared.Dtos.TalentProfiles;
using Shared.Exceptions;
using Modules.Jobs.GetJobApplicantsSummary;
using Modules.Jobs.GetApplicantResume;
using Modules.Jobs.GetTalentsNotApplied;
using Modules.Jobs.GetTalentProfileTextInfo;
using Modules.Jobs.InviteTalent;

namespace Host.API.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public sealed class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet("public")]
    [ProducesResponseType(typeof(GetPublicJobsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicJobsAsync(
            [FromQuery] string? searchTerm,
            [FromQuery] string? employmentType,
            [FromQuery] string? workArrangement,
            [FromQuery] string? experienceLevel,
            [FromQuery] int page = 1,
            CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetPublicJobsQuery(searchTerm, employmentType, workArrangement, experienceLevel, page, 12), cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(GetJobDetailsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetJobByIdQuery(Guid.Empty, jobId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(GetEmployerDashboardResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardAsync(
        [FromQuery] string? searchTerm,
        [FromQuery] string? statusFilter,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();

        var response = await _mediator.Send(new GetEmployerDashboardQuery(userId, searchTerm, statusFilter, page, 10), cancellationToken);
        return Ok(response);
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateJobDescriptionResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateAsync([FromBody] GenerateJobDescriptionRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _mediator.Send(new GenerateJobDescriptionCommand(
            userId, request.JobId, request.Title, request.Description, request.Responsibilities, request.Skills,
            request.EmploymentType, request.WorkArrangement, request.ExperienceLevel,
            request.ExperienceMinYears, request.ExperienceMaxYears, request.SalaryMin, request.SalaryMax,
            request.CountryCode, request.City, request.Timezone, request.CurrencyCode, request.NumberOfPositions,
            request.ApplicationDeadline), cancellationToken);
        return Ok(response);
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateJobResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateJobRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _mediator.Send(new CreateJobCommand(
            userId, request.PublishJob, request.Title, request.Description, request.Responsibilities,
            request.Skills, request.EmploymentType, request.WorkArrangement, request.ExperienceLevel,
            request.ExperienceMinYears, request.ExperienceMaxYears, request.SalaryMin, request.SalaryMax,
            request.CountryCode, request.City, request.Timezone, request.CurrencyCode, request.NumberOfPositions,
            request.ApplicationDeadline), cancellationToken);
        return Ok(response);
    }

    [HttpPut("{jobId:guid}")]
    [ProducesResponseType(typeof(CreateJobResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(Guid jobId, [FromBody] UpdateJobRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _mediator.Send(new UpdateJobCommand(
            userId, jobId, request.PublishJob, request.Title, request.Description, request.Responsibilities,
            request.Skills, request.EmploymentType, request.WorkArrangement, request.ExperienceLevel,
            request.ExperienceMinYears, request.ExperienceMaxYears, request.SalaryMin, request.SalaryMax,
            request.CountryCode, request.City, request.Timezone, request.CurrencyCode, request.NumberOfPositions,
            request.ApplicationDeadline), cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{jobId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatusAsync(Guid jobId, [FromBody] UpdateJobStatusRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        await _mediator.Send(new UpdateJobStatusCommand(userId, jobId, request.Action), cancellationToken);
        return Ok();
    }

    [HttpDelete("{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        await _mediator.Send(new DeleteJobCommand(userId, jobId), cancellationToken);
        return Ok();
    }

    [HttpPatch("applicants/{applicationId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateApplicationStatusAsync(Guid applicationId, [FromBody] UpdateApplicationStatusRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        await _mediator.Send(new UpdateApplicationStatusCommand(userId, applicationId, request.Status), cancellationToken);
        return Ok();
    }

    [HttpGet("{jobId}/applicants")]
    [ProducesResponseType(typeof(GetJobApplicantsSummaryResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobApplicantsSummaryAsync(
        [FromRoute] Guid jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new GetJobApplicantsSummaryQuery(jobId, userId, page, pageSize, status, searchTerm), cancellationToken);
        return Ok(result);
    }

    [HttpGet("applicants/{applicationId}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicantResumeAsync(
        [FromRoute] Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var download = await _mediator.Send(new GetApplicantResumeQuery(applicationId, userId), cancellationToken);
        
        return File(download.Stream, download.ContentType, "resume.pdf");
    }

    [HttpGet("{jobId}/talents")]
    [ProducesResponseType(typeof(GetTalentsNotAppliedResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTalentsNotAppliedAsync(
        [FromRoute] Guid jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? minExperience = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var response = await _mediator.Send(new GetTalentsNotAppliedQuery(jobId, userId, page, pageSize, searchTerm, minExperience), cancellationToken);
        return Ok(response);
    }

    [HttpGet("talents/{talentId}")]
    [ProducesResponseType(typeof(TalentProfileTextInfoResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTalentProfileAsync(
        [FromRoute] Guid talentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new GetTalentProfileTextInfoQuery(talentId), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{jobId:guid}/talents/{talentId:guid}/invite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InviteAsync(
        [FromRoute] Guid jobId,
        [FromRoute] Guid talentId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        await _mediator.Send(new InviteTalentCommand(userId, jobId, talentId), cancellationToken);
        return Ok(new { Message = "Invitation has been sent successfully." });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("Invalid user context.");
        }

        return userId;
    }
}