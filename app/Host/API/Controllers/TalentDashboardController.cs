using System.Security.Claims;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.TalentDashboard.GetApplications;
using Shared.Dtos.TalentDashboard;

namespace Host.API.Controllers;

[ApiController]
[Route("api/v1/talent-dashboard")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class TalentDashboardController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("applications")]
    [ProducesResponseType(typeof(GetTalentDashboardApplicationsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicationsAsync(
        [FromQuery] int applicationsPage = 1,
        [FromQuery] int invitationsPage = 1,
        [FromQuery] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        var response = await _mediator.Send(
            new GetTalentDashboardApplicationsQuery(
                userId.Value,
                applicationsPage,
                invitationsPage,
                pageSize),
            cancellationToken);

        return Ok(response);
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
