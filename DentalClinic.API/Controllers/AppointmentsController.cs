using DentalClinic.API.Extensions;
using DentalClinic.API.Models;
using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Appointments;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Controllers;

/// <summary>ניהול תורים. מנהל רואה ומנהל הכול; מטופל - רק את התורים שלו (נאכף בשרת, בשכבת ה-Service).</summary>
[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>שליפת רשימת התורים עם דפדוף, סינון ומיון.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AppointmentResponseDto>), StatusCodes.Status200OK)]
    //האטריביוט [ProducesResponseType]
    //מצהיר מראש אילו קודי סטטוס HTTP (..200, 400, 404)
    //ואיזה סוג מבני נתונים הפונקציה תחזיר בתגובה לבקשה.
    //הפונקציה עובדת רק בסטטוס 200- הצלחה
    public async Task<ActionResult<PagedResult<AppointmentResponseDto>>> GetAll([FromQuery] AppointmentQuery query, CancellationToken ct)
    {
        return Ok(await _appointmentService.GetAllAsync(query, User.ToCurrentUser(), ct));
    }

    /// <summary>שליפת פרטי תור לפי מזהה.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]//הגישה לפונקציה עלולה להיחסם אם למשתמש המחובר אין הרשאות מתאימות.
    [ProducesResponseType(StatusCodes.Status404NotFound)]// שהפונקציה עשויה להחזיר שגיאה שהמשאב לא נמצא למשל אם התור המבוקש אינו קיים במערכת.
    public async Task<ActionResult<AppointmentResponseDto>> GetById([FromRoute] int id, CancellationToken ct)
    {
        return Ok(await _appointmentService.GetByIdAsync(id, User.ToCurrentUser(), ct));
    }

    /// <summary>
    /// קביעת תור חדש: 201 עם Location אם הצליח, 400 אם המועד מחוץ לשעות הפעילות,
    /// ו-409 Conflict אם השעה נתפסה על ידי משתמש אחר (כולל קביעה בו-זמנית של שני משתמשים).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.Patient)]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentResponseDto>> Create([FromBody] AppointmentCreateDto dto, CancellationToken ct)
    {
        var created = await _appointmentService.CreateAsync(dto, User.ToCurrentUser(), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>מחיקת תור לפי id. מחזיר 204.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _appointmentService.DeleteAsync(id, User.ToCurrentUser(), ct);
        return NoContent();
    }
}
