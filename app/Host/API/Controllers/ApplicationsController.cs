using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Applications.Create;
using Shared.Dtos.Applications;

namespace Host.API.Controllers;

[ApiController]
[Route("api/v1/applications")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IResumeExtractionService _resumeExtractionService;

    public ApplicationsController(
        IMediator mediator,
        IResumeExtractionService resumeExtractionService)
    {
        _mediator = mediator;
        _resumeExtractionService = resumeExtractionService;
    }

    [AllowAnonymous]
    [HttpPost("extract")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ExtractApplicationResumeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExtractAsync([FromForm] UploadApplicationResumeDto request)
    {
        await using var resumeStream = request.Resume.OpenReadStream();

        var response = await _resumeExtractionService.ExtractAsync(
            resumeStream,
            request.Resume.FileName,
            request.Resume.ContentType,
            HttpContext.RequestAborted);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateApplicationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromForm] CreateApplicationRequestDto request)
    {
        var response = await _mediator.Send(new CreateApplicationCommand(
            request.JobId,
            request.Name,
            request.Email,
            request.ContactNumber,
            request.Education,
            request.Department,
            request.CGPA,
            request.LinkedInUrl,
            request.AdditionalUrl,
            request.Location,
            request.CoverLetter,
            request.ExpectedSalary,
            request.Experience,
            request.Resume));

        return Created("/api/v1/applications", response);
    }
}
