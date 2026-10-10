using DentalClinic.API.Extensions;
using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Appointments;
using DentalClinic.Core.DTOs.Patients;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Controllers;

/// <summary>ניהול מטופלים. Controller דק: רק קבלת הבקשה, העברה ל-Service והחזרת סטטוס.</summary>
[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>שליפת כל המטופלים עם דפדוף, חיפוש ומיון (מנהל בלבד).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PagedResult<PatientResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PatientResponseDto>>> GetAll([FromQuery] PatientQuery query, CancellationToken ct)
    {
        return Ok(await _patientService.GetAllAsync(query, ct));
    }

    /// <summary>שליפת פרטי מטופל לפי מזהה, כולל History (מנהל, או המטופל עצמו).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PatientDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetailsDto>> GetById([FromRoute] int id, CancellationToken ct)
    {
        return Ok(await _patientService.GetByIdAsync(id, User.ToCurrentUser(), ct));
    }

    /// <summary>הוספת מטופל חדש (מנהל בלבד). מחזיר 201 עם Location.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatientResponseDto>> Create([FromBody] PatientRequestDto dto, CancellationToken ct)
    {
        var created = await _patientService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>עדכון פרטי מטופל (מנהל, או המטופל עצמו). מחזיר 204.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PatientRequestDto dto, CancellationToken ct)
    {
        await _patientService.UpdateAsync(id, dto, User.ToCurrentUser(), ct);
        return NoContent();
    }

    /// <summary>שליפת כל התורים שקבע המטופל לפי המזהה שלו.</summary>
    [HttpGet("{id:int}/appointments")]
    [ProducesResponseType(typeof(List<AppointmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<AppointmentResponseDto>>> GetAppointments([FromRoute] int id, CancellationToken ct)
    {
        return Ok(await _patientService.GetAppointmentsAsync(id, User.ToCurrentUser(), ct));
    }
}
