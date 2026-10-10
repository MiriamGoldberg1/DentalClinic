using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Doctors;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Controllers;

/// <summary>ניהול רופאים. צפייה - לכל משתמש מחובר; הוספה ועדכון - מנהל בלבד.</summary>
[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;
    private readonly IAppointmentService _appointmentService;

    public DoctorsController(IDoctorService doctorService, IAppointmentService appointmentService)
    {
        _doctorService = doctorService;
        _appointmentService = appointmentService;
    }

    /// <summary>שליפת כל הרופאים עם דפדוף, חיפוש, סינון לפי התמחות ומיון.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DoctorResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DoctorResponseDto>>> GetAll([FromQuery] DoctorQuery query, CancellationToken ct)
    {
        return Ok(await _doctorService.GetAllAsync(query, ct));
    }

    /// <summary>שליפת פרטי רופא לפי מזהה.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DoctorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorResponseDto>> GetById([FromRoute] int id, CancellationToken ct)
    {
        return Ok(await _doctorService.GetByIdAsync(id, ct));
    }

    /// <summary>הוספת רופא חדש (מנהל בלבד). מחזיר 201 עם Location.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(DoctorResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DoctorResponseDto>> Create([FromBody] DoctorRequestDto dto, CancellationToken ct)
    {
        var created = await _doctorService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>עדכון פרטי רופא (מנהל בלבד). מחזיר 204, או 409 אם היומן שלו עודכן באותו רגע.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] DoctorRequestDto dto, CancellationToken ct)
    {
        await _doctorService.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>השעות הפנויות ביומן הרופא בתאריך מסוים </summary>
    [HttpGet("{id:int}/available-slots")]
    [ProducesResponseType(typeof(AvailableSlotsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailableSlotsDto>> GetAvailableSlots([FromRoute] int id, [FromQuery] DateTime date, CancellationToken ct)
    {
        return Ok(await _appointmentService.GetAvailableSlotsAsync(id, DateOnly.FromDateTime(date), ct));
    }
}
