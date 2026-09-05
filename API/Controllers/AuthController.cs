using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthController(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    private string GetCorrelationId() =>
        HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

    /// <summary>
    /// Validate credentials and issue a JWT.
    /// Invalid credentials return the same 401 without revealing whether the email exists.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new Application.DTOs.Common.ErrorResponse { Code = "INVALID_CREDENTIALS", Message = "Invalid email or password.", CorrelationId = GetCorrelationId() });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Customer";

        var response = _tokenService.GenerateToken(user, role);

        Response.Cookies.Append("access_token", response.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = response.ExpiresAtUtc
        });

        return Ok(response);
    }

    /// <summary>
    /// Register a new customer account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new Application.DTOs.Common.ErrorResponse { Code = "EMAIL_TAKEN", Message = "An account with this email already exists.", CorrelationId = GetCorrelationId() });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new Application.DTOs.Common.ErrorResponse { Code = "REGISTRATION_FAILED", Message = errors, CorrelationId = GetCorrelationId() });
        }

        // New registrations are always Customer role
        await _userManager.AddToRoleAsync(user, "Customer");

        var tokenResponse = _tokenService.GenerateToken(user, "Customer");
        return CreatedAtAction(nameof(Login), tokenResponse);
    }

    /// <summary>
    /// Request a password reset token. Since email is not required,
    /// the token is returned directly in the response (dev-only).
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal whether email exists — return 200 regardless
            return Ok(new ForgotPasswordResponse { Message = "If the email exists, a reset token has been generated." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // In production, this would be sent via email. For dev, return directly.
        return Ok(new ForgotPasswordResponse { Message = "Password reset token generated.", ResetToken = token });
    }

    /// <summary>
    /// Reset password using a token.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new Application.DTOs.Common.ErrorResponse { Code = "INVALID_REQUEST", Message = "Invalid request.", CorrelationId = GetCorrelationId() });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new Application.DTOs.Common.ErrorResponse { Code = "RESET_FAILED", Message = errors, CorrelationId = GetCorrelationId() });
        }

        return Ok(new Application.DTOs.Common.SuccessResponse { Message = "Password has been reset successfully." });
    }
}
