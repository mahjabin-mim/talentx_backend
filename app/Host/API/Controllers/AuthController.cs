using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.Auth.ChangePassword;
using Modules.Auth.GoogleAuth;
using Modules.Auth.Login;
using Modules.Auth.Logout;
using Modules.Auth.RefreshToken;
using Modules.Auth.ResendVerificationOtp;
using Modules.Auth.Register;
using Modules.Auth.UpdateUserRole;
using Modules.Auth.VerifyEmail;
using Shared.Dtos.Auth;

namespace Host.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new RegisterUserCommand(
            request.Email,
            request.Password,
            request.Fullname,
            request.CompanyName,
            request.PhoneNumber), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(VerifyEmailResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new VerifyEmailCommand(
            request.Email,
            request.OtpCode), cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("resend-verification-otp")]
    [ProducesResponseType(typeof(ResendVerificationOtpResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerificationOtpAsync([FromBody] ResendVerificationOtpRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ResendVerificationOtpCommand(
            request.Email), cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new LoginUserCommand(
            request.Email,
            request.Password), cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new RefreshTokenCommand(
            request.UserId,
            request.RefreshToken), cancellationToken);

        return Ok(response);
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        await _mediator.Send(new ChangePasswordCommand(
            userId.Value,
            request.CurrentPassword,
            request.NewPassword), cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        await _mediator.Send(new LogoutUserCommand(userId.Value), cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("role")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRoleAsync([FromBody] UpdateUserRoleRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return InvalidUserContextProblem();
        }

        var response = await _mediator.Send(new UpdateUserRoleCommand(
            userId.Value,
            request.UserRole,
            request.Fullname,
            request.CompanyName), cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("oauth/google")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GoogleAuthAsync([FromBody] GoogleAuthRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GoogleAuthCommand(
            request.Email,
            request.Fullname,
            request.ProfileImageURL), cancellationToken);

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
