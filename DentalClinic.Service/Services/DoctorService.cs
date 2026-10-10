using AutoMapper;
using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Doctors;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Exceptions;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Core.Interfaces.Services;
using DentalClinic.Service.Mapping;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Service.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctors;
    private readonly ISpecializationRepository _specializations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(IDoctorRepository doctors, ISpecializationRepository specializations, IUnitOfWork unitOfWork,
        IMapper mapper, IDateTimeProvider clock, ILogger<DoctorService> logger)
    {
        _doctors = doctors;
        _specializations = specializations;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>שליפת כל הרופאים (עם דפדוף, חיפוש, סינון לפי התמחות ומיון).</summary>
    public async Task<PagedResult<DoctorResponseDto>> GetAllAsync(DoctorQuery query, CancellationToken ct)
    {
        var result = await _doctors.GetPagedAsync(query, ct);
        return _mapper.MapPaged<Doctor, DoctorResponseDto>(result);
    }

    /// <summary>שליפת פרטי רופא לפי מזהה.</summary>
    public async Task<DoctorResponseDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var doctor = await _doctors.GetByIdAsync(id, ct)
                     ?? throw new NotFoundException($"רופא {id} לא נמצא");
        return _mapper.Map<DoctorResponseDto>(doctor);
    }

    /// <summary>הוספת רופא חדש, כולל קישור לתחומי ההתמחות שלו.</summary>
    public async Task<DoctorResponseDto> CreateAsync(DoctorRequestDto dto, CancellationToken ct)
    {
        ValidateDates(dto);

        var doctor = _mapper.Map<Doctor>(dto);
        _mapper.Map(dto.Address, doctor.Address);
        doctor.Specializations = await LoadSpecializationsAsync(dto.SpecializationIds, ct);

        _doctors.Add(doctor);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Doctor {DoctorId} created", doctor.Id);
        return _mapper.Map<DoctorResponseDto>(doctor);
    }

    /// <summary>
    /// עדכון פרטי רופא. גם העדכון עובר דרך ה-Concurrency Token של הרופא:
    /// אם באותו רגע נקבע תור ביומן שלו, אחד מהשניים יקבל 409 ויתבקש לנסות שוב.
    /// </summary>
    public async Task UpdateAsync(int id, DoctorRequestDto dto, CancellationToken ct)
    {
        ValidateDates(dto);

        var doctor = await _doctors.GetForUpdateAsync(id, ct)
                     ?? throw new NotFoundException($"רופא {id} לא נמצא");

        _mapper.Map(dto, doctor);
        _mapper.Map(dto.Address, doctor.Address);

        var specializations = await LoadSpecializationsAsync(dto.SpecializationIds, ct);
        doctor.Specializations.Clear();
        foreach (var specialization in specializations)
        {
            doctor.Specializations.Add(specialization);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Doctor {DoctorId} updated", id);
    }

    /// <summary>טוען את ההתמחויות לפי מזהים ומוודא שכולן קיימות.</summary>
    private async Task<List<Specialization>> LoadSpecializationsAsync(List<int> ids, CancellationToken ct)
    {
        var distinctIds = ids.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return new List<Specialization>();
        }

        var specializations = await _specializations.GetByIdsAsync(distinctIds, ct);
        if (specializations.Count != distinctIds.Count)
        {
            throw new BusinessRuleException("אחד או יותר מתחומי ההתמחות אינו קיים");
        }

        return specializations;
    }

    /// <summary>תאריך לידה בעבר; תחילת העבודה לא לפני גיל 18 ולא בעתיד הרחוק.</summary>
    private void ValidateDates(DoctorRequestDto dto)
    {
        var today = _clock.Today;
        if (dto.DateOfBirth >= today || dto.DateOfBirth.Year < 1900)
        {
            throw new BusinessRuleException("תאריך הלידה אינו תקין");
        }

        if (dto.EmploymentStartDate < dto.DateOfBirth.AddYears(18) || dto.EmploymentStartDate > today.AddYears(1))
        {
            throw new BusinessRuleException("תאריך תחילת העבודה במרפאה אינו תקין");
        }
    }
}
