using AutoMapper;
using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Appointments;
using DentalClinic.Core.DTOs.Doctors;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Exceptions;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Core.Interfaces.Services;
using DentalClinic.Core.Settings;
using DentalClinic.Service.Mapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.Service.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointments;
    private readonly IDoctorRepository _doctors;
    private readonly IPatientRepository _patients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _clock;
    private readonly ClinicSettings _settings;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(IAppointmentRepository appointments, IDoctorRepository doctors, IPatientRepository patients,
        IUnitOfWork unitOfWork, IMapper mapper, IDateTimeProvider clock, IOptions<ClinicSettings> settings,
        ILogger<AppointmentService> logger)
    {
        _appointments = appointments;
        _doctors = doctors;
        _patients = patients;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _clock = clock;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>שליפת רשימת התורים עם דפדוף. מטופל רואה רק את התורים שלו, גם אם ביקש אחרת.</summary>
    public async Task<PagedResult<AppointmentResponseDto>> GetAllAsync(AppointmentQuery query, CurrentUser user, CancellationToken ct)
    {
        if (!user.IsAdmin)
        {
            query.PatientId = user.PatientId ?? throw new ForbiddenException("למשתמש אין רשומת מטופל");
        }

        var result = await _appointments.GetPagedAsync(query, ct);
        return _mapper.MapPaged<Appointment, AppointmentResponseDto>(result);
    }

    /// <summary>שליפת פרטי תור לפי מזהה. מטופל רשאי לראות רק תור שלו.</summary>
    public async Task<AppointmentResponseDto> GetByIdAsync(int id, CurrentUser user, CancellationToken ct)
    {
        var appointment = await _appointments.GetByIdAsync(id, ct)
                          ?? throw new NotFoundException($"תור {id} לא נמצא");
        EnsureOwner(appointment.PatientId, user);
        return _mapper.Map<AppointmentResponseDto>(appointment);
    }

    /// <summary>
    /// קביעת תור - הטיפול בתחרות על המשאב המוגבל (שעה ביומן של רופא): קוראים את הרופא כולל Version, בודקים שהשעה פנויה,
    /// מוסיפים תור ומחליפים Version. השמירה היא טרנזקציה אחת (INSERT + UPDATE ... WHERE Version = הישן); אם מישהו שינה
    /// את היומן בינתיים - התנגשות, מנקים ומנסים שוב עם נתונים טריים. שעה תפוסה או ניסיונות שנגמרו => 409.
    /// </summary>
    public async Task<AppointmentResponseDto> CreateAsync(AppointmentCreateDto dto, CurrentUser user, CancellationToken ct)
    {
        var patientId = ResolvePatientId(dto, user);
        ValidateScheduleRules(dto.Date, dto.Time);

        if (!await _patients.ExistsAsync(patientId, ct))
        {
            throw new NotFoundException($"מטופל {patientId} לא נמצא");
        }

        var maxAttempts = Math.Max(1, _settings.MaxBookingAttempts);
        for (var attempt = 1; ; attempt++)
        {
            // שלב 1: קריאת הרופא - הגרסה הנוכחית של היומן היא נקודת הייחוס
            var doctor = await _doctors.GetForBookingAsync(dto.DoctorId, ct)
                         ?? throw new NotFoundException($"רופא {dto.DoctorId} לא נמצא");

            if (dto.Date < doctor.EmploymentStartDate)
            {
                throw new BusinessRuleException("הרופא עדיין לא התחיל לעבוד במרפאה בתאריך זה");
            }

            // שלב 2: בדיקה עסקית - האם השעה פנויה אצל הרופא ואצל המטופל
            if (await _appointments.IsDoctorSlotTakenAsync(doctor.Id, dto.Date, dto.Time, ct))
            {
                _logger.LogWarning("Slot already taken: doctor {DoctorId} {Date} {Time}", doctor.Id, dto.Date, dto.Time);
                throw new ConflictException("התור שבחרת כבר נתפס. אנא בחר שעה אחרת.");
            }

            if (await _appointments.IsPatientSlotTakenAsync(patientId, dto.Date, dto.Time, ct))
            {
                throw new ConflictException("כבר קיים לך תור בתאריך ובשעה אלו.");
            }

            // שלב 3: הוספת התור ו"נגיעה" ביומן הרופא - Version חדש
            var appointment = new Appointment
            {
                Date = dto.Date,
                Time = dto.Time,
                DoctorId = doctor.Id,
                PatientId = patientId,
                TreatmentType = dto.TreatmentType,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                CreatedAtUtc = _clock.UtcNow
            };
            _appointments.Add(appointment);
            doctor.Version = Guid.NewGuid();

            try
            {
                // שלב 4: שמירה אטומית. גרסה ישנה => ConcurrencyConflictException
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("Appointment {AppointmentId} booked: doctor {DoctorId}, patient {PatientId}, {Date} {Time}",
                    appointment.Id, doctor.Id, patientId, dto.Date, dto.Time);

                var created = await _appointments.GetByIdAsync(appointment.Id, ct)
                              ?? throw new NotFoundException($"תור {appointment.Id} לא נמצא");
                return _mapper.Map<AppointmentResponseDto>(created);
            }
            catch (ConcurrencyConflictException)
            {
                // התנגשות: משתמש אחר שינה את יומן הרופא בין הקריאה לשמירה. מנקים ומנסים שוב
                _unitOfWork.DiscardChanges();
                _logger.LogWarning("Concurrency conflict on doctor {DoctorId} schedule, attempt {Attempt}/{MaxAttempts}",
                    doctor.Id, attempt, maxAttempts);

                if (attempt >= maxAttempts)
                {
                    throw new ConflictException("יומן הרופא עודכן על ידי משתמש אחר באותו רגע. אנא נסה שוב.");
                }
            }
        }
    }

    /// <summary>מחיקת תור. מטופל רשאי למחוק רק תור שלו.</summary>
    public async Task DeleteAsync(int id, CurrentUser user, CancellationToken ct)
    {
        var appointment = await _appointments.GetForDeleteAsync(id, ct)
                          ?? throw new NotFoundException($"תור {id} לא נמצא");
        EnsureOwner(appointment.PatientId, user);

        _appointments.Remove(appointment);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Appointment {AppointmentId} deleted", id);
    }

    /// <summary>השעות הפנויות אצל רופא בתאריך מסוים (ריק בימים שאינם א'-ה' ובתאריך שעבר).</summary>
    public async Task<AvailableSlotsDto> GetAvailableSlotsAsync(int doctorId, DateOnly date, CancellationToken ct)
    {
        if (!await _doctors.ExistsAsync(doctorId, ct))
        {
            throw new NotFoundException($"רופא {doctorId} לא נמצא");
        }

        var result = new AvailableSlotsDto { DoctorId = doctorId, Date = date };
        if (!ClinicHours.IsWorkingDay(date) || date < _clock.Today)
        {
            return result;
        }

        var booked = (await _appointments.GetDoctorBookedTimesAsync(doctorId, date, ct)).ToHashSet();
        var isToday = date == _clock.Today;
        var now = _clock.CurrentTime;

        result.Slots = ClinicHours.AllSlots()
            .Where(slot => !booked.Contains(slot) && (!isToday || slot > now))
            .ToList();
        return result;
    }

    /// <summary>מנהל חייב לציין מטופל; מטופל קובע תור רק לעצמו (המזהה נלקח מה-JWT).</summary>
    private static int ResolvePatientId(AppointmentCreateDto dto, CurrentUser user)
    {
        if (user.IsAdmin)
        {
            return dto.PatientId ?? throw new BusinessRuleException("יש לבחור מטופל עבורו נקבע התור");
        }

        if (user.PatientId is null)
        {
            throw new ForbiddenException("למשתמש אין רשומת מטופל");
        }

        if (dto.PatientId.HasValue && dto.PatientId.Value != user.PatientId.Value)
        {
            throw new ForbiddenException("מטופל יכול לקבוע תור רק לעצמו");
        }

        return user.PatientId.Value;
    }

    /// <summary>כללי הזמנים: ימים א'-ה' בלבד, 11:00-18:00, בחלונות של 30 דקות, ולא בעבר.</summary>
    private void ValidateScheduleRules(DateOnly date, TimeOnly time)
    {
        if (!ClinicHours.IsWorkingDay(date))
        {
            throw new BusinessRuleException("ניתן לקבוע תורים רק בימים ראשון עד חמישי");
        }

        if (!ClinicHours.IsValidStartTime(time))
        {
            throw new BusinessRuleException(
                $"ניתן לקבוע תורים רק בין {ClinicHours.Opening:HH:mm} ל-{ClinicHours.Closing:HH:mm}, בחלונות של {ClinicHours.AppointmentLengthMinutes} דקות");
        }

        var today = _clock.Today;
        if (date < today || (date == today && time <= _clock.CurrentTime))
        {
            throw new BusinessRuleException("לא ניתן לקבוע תור במועד שכבר עבר");
        }
    }

    /// <summary>הרשאה ברמת השרת: מנהל - הכול, מטופל - רק התורים שלו.</summary>
    private static void EnsureOwner(int appointmentPatientId, CurrentUser user)
    {
        if (!user.IsAdmin && user.PatientId != appointmentPatientId)
        {
            throw new ForbiddenException("אין הרשאה לגשת לתור של מטופל אחר");
        }
    }
}
