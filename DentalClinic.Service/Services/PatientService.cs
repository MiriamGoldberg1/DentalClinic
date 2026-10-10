using AutoMapper;
using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Appointments;
using DentalClinic.Core.DTOs.Patients;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Exceptions;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Core.Interfaces.Services;
using DentalClinic.Service.Mapping;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Service.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patients;
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<PatientService> _logger;

    public PatientService(IPatientRepository patients, IAppointmentRepository appointments, IUnitOfWork unitOfWork,
        IMapper mapper, IDateTimeProvider clock, ILogger<PatientService> logger)
    {
        _patients = patients;
        _appointments = appointments;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>שליפת כל המטופלים (עם דפדוף, חיפוש ומיון).</summary>
    public async Task<PagedResult<PatientResponseDto>> GetAllAsync(PatientQuery query, CancellationToken ct)
    {
        var result = await _patients.GetPagedAsync(query, ct);
        return _mapper.MapPaged<Patient, PatientResponseDto>(result);
    }

    /// <summary>שליפת פרטי מטופל לפי מזהה, כולל History. מטופל רשאי לראות רק את עצמו.</summary>
    public async Task<PatientDetailsDto> GetByIdAsync(int id, CurrentUser user, CancellationToken ct)
    {
        EnsureCanAccessPatient(id, user);
        var patient = await _patients.GetByIdWithHistoryAsync(id, ct)
                      ?? throw new NotFoundException($"מטופל {id} לא נמצא");
        return _mapper.Map<PatientDetailsDto>(patient);
    }

    /// <summary>הוספת מטופל חדש (מנהל בלבד - נאכף ב-Controller).</summary>
    public async Task<PatientResponseDto> CreateAsync(PatientRequestDto dto, CancellationToken ct)
    {
        ValidateDateOfBirth(dto.DateOfBirth);

        var patient = _mapper.Map<Patient>(dto);
        _mapper.Map(dto.Address, patient.Address);
        _patients.Add(patient);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientId} created", patient.Id);
        return _mapper.Map<PatientResponseDto>(patient);
    }

    /// <summary>עדכון פרטי מטופל. מטופל רשאי לעדכן רק את עצמו.</summary>
    public async Task UpdateAsync(int id, PatientRequestDto dto, CurrentUser user, CancellationToken ct)
    {
        EnsureCanAccessPatient(id, user);
        ValidateDateOfBirth(dto.DateOfBirth);

        var patient = await _patients.GetForUpdateAsync(id, ct)
                      ?? throw new NotFoundException($"מטופל {id} לא נמצא");

        _mapper.Map(dto, patient);
        _mapper.Map(dto.Address, patient.Address);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientId} updated", id);
    }

    /// <summary>שליפת כל התורים שקבע מטופל מסוים לפי המזהה שלו.</summary>
    public async Task<List<AppointmentResponseDto>> GetAppointmentsAsync(int patientId, CurrentUser user, CancellationToken ct)
    {
        EnsureCanAccessPatient(patientId, user);
        if (!await _patients.ExistsAsync(patientId, ct))
        {
            throw new NotFoundException($"מטופל {patientId} לא נמצא");
        }

        var appointments = await _appointments.GetByPatientIdAsync(patientId, ct);
        return _mapper.Map<List<AppointmentResponseDto>>(appointments);
    }

    /// <summary>הרשאה ברמת השרת: מנהל רואה הכול, מטופל רק את הרשומה שלו.</summary>
    private static void EnsureCanAccessPatient(int patientId, CurrentUser user)
    {
        if (!user.IsAdmin && user.PatientId != patientId)
        {
            throw new ForbiddenException("אין הרשאה לגשת לנתוני מטופל אחר");
        }
    }

    /// <summary>תאריך לידה חייב להיות בעבר ולא לפני 1900.</summary>
    private void ValidateDateOfBirth(DateOnly dateOfBirth)
    {
        if (dateOfBirth >= _clock.Today || dateOfBirth.Year < 1900)
        {
            throw new BusinessRuleException("תאריך הלידה אינו תקין");
        }
    }
}
