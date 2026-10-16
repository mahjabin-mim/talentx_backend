using Modules.Employer.CreateProfile;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Host.API.Controllers;

[ApiController]
[Route("api/v1/employer")]
public class EmployerController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public EmployerController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [Authorize]
    [HttpPost("create-profile")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateProfileAsync([FromBody] CreateEmployerProfileRequestDto request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _mediator.Send(new CreateEmployerProfileCommand(userId, request.FullName, request.CompanyName), cancellationToken);

        return StatusCode(StatusCodes.Status201Created);
    }
}
