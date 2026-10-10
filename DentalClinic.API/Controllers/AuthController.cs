using DentalClinic.Core.DTOs.Auth;
using DentalClinic.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Controllers;

/// <summary>הרשמה והתחברות - מחזירים JWT שהלקוח שולח ב-header בשם Authorization.</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>הרשמת מטופל חדש. מחזיר 201 עם Location לרשומת המטופל שנוצרה, ו-409 אם האימייל קיים.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var response = await _authService.RegisterAsync(dto, ct);
        return CreatedAtAction(nameof(PatientsController.GetById), "Patients", new { id = response.PatientId }, response);
    }

    /// <summary>התחברות עם אימייל וסיסמה. 401 אם הפרטים שגויים.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        return Ok(await _authService.LoginAsync(dto, ct));
    }
}
