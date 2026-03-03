using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projects_menagment.Api.Dtos.Auth;
using projects_menagment.Api.Dtos.Common;
using projects_menagment.Application.Dtos.Auth;
using projects_menagment.Application.Exceptions;
using projects_menagment.Application.Interfaces.Services;

namespace projects_menagment.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("signup")]
    [ProducesResponseType(typeof(SignupSuccessResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Signup([FromBody] SignupRequestBodyDto? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException("Request body is required.");
        }

        logger.LogInformation("Processing signup request for email {Email}", request.Email);

        var response = await authService.SignupAsync(
            new SignupRequestDto(
                request.FirstName ?? string.Empty,
                request.LastName ?? string.Empty,
                request.Email ?? string.Empty,
                request.Password ?? string.Empty),
            cancellationToken);

        logger.LogInformation("Signup completed successfully for user {UserId}", response.UserId);

        return StatusCode(
            StatusCodes.Status201Created,
            new SignupSuccessResponseDto(response.UserId, response.Message));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseBodyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequestBodyDto? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException("Request body is required.");
        }

        logger.LogInformation("Processing login request for email {Email}", request.Email);

        var response = await authService.LoginAsync(
            new LoginRequestDto(
                request.Email ?? string.Empty,
                request.Password ?? string.Empty),
            cancellationToken);

        return Ok(new LoginResponseBodyDto(
            response.AccessToken,
            response.RefreshToken,
            response.AccessTokenExpiresAt,
            response.RefreshTokenExpiresAt));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponseBodyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestBodyDto? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException("Request body is required.");
        }

        var response = await authService.RefreshTokenAsync(
            new RefreshTokenRequestDto(request.RefreshToken ?? string.Empty),
            cancellationToken);

        return Ok(new LoginResponseBodyDto(
            response.AccessToken,
            response.RefreshToken,
            response.AccessTokenExpiresAt,
            response.RefreshTokenExpiresAt));
    }
}
