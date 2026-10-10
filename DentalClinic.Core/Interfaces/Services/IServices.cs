using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Appointments;
using DentalClinic.Core.DTOs.Auth;
using DentalClinic.Core.DTOs.Common;
using DentalClinic.Core.DTOs.Doctors;
using DentalClinic.Core.DTOs.Patients;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Entities;

namespace DentalClinic.Core.Interfaces.Services;

/// <summary>לוגיקה עסקית של מטופלים.</summary>
public interface IPatientService
{
    Task<PagedResult<PatientResponseDto>> GetAllAsync(PatientQuery query, CancellationToken ct);
    Task<PatientDetailsDto> GetByIdAsync(int id, CurrentUser user, CancellationToken ct);
    Task<PatientResponseDto> CreateAsync(PatientRequestDto dto, CancellationToken ct);
    Task UpdateAsync(int id, PatientRequestDto dto, CurrentUser user, CancellationToken ct);
    Task<List<AppointmentResponseDto>> GetAppointmentsAsync(int patientId, CurrentUser user, CancellationToken ct);
}

/// <summary>לוגיקה עסקית של רופאים.</summary>
public interface IDoctorService
{
    Task<PagedResult<DoctorResponseDto>> GetAllAsync(DoctorQuery query, CancellationToken ct);
    Task<DoctorResponseDto> GetByIdAsync(int id, CancellationToken ct);
    Task<DoctorResponseDto> CreateAsync(DoctorRequestDto dto, CancellationToken ct);
    Task UpdateAsync(int id, DoctorRequestDto dto, CancellationToken ct);
}

/// <summary>לוגיקה עסקית של תורים - כולל הטיפול בתחרות על יומן הרופא.</summary>
public interface IAppointmentService
{
    Task<PagedResult<AppointmentResponseDto>> GetAllAsync(AppointmentQuery query, CurrentUser user, CancellationToken ct);
    Task<AppointmentResponseDto> GetByIdAsync(int id, CurrentUser user, CancellationToken ct);
    Task<AppointmentResponseDto> CreateAsync(AppointmentCreateDto dto, CurrentUser user, CancellationToken ct);
    Task DeleteAsync(int id, CurrentUser user, CancellationToken ct);
    Task<AvailableSlotsDto> GetAvailableSlotsAsync(int doctorId, DateOnly date, CancellationToken ct);
}

/// <summary>תחומי התמחות.</summary>
public interface ISpecializationService
{
    Task<List<SpecializationDto>> GetAllAsync(CancellationToken ct);
}

/// <summary>הרשמה והתחברות.</summary>
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct);
}

/// <summary>יצירת JWT עבור משתמש.</summary>
public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}

/// <summary>גיבוב ואימות סיסמאות.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>שעון המערכת - מאפשר להחליף את "עכשיו" בבדיקות.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
    TimeOnly CurrentTime { get; }
}
