using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Entities;

namespace DentalClinic.Core.Interfaces.Repositories;

/// <summary>גישה לנתוני מטופלים.</summary>
public interface IPatientRepository
{
    Task<PagedResult<Patient>> GetPagedAsync(PatientQuery query, CancellationToken ct);
    Task<Patient?> GetByIdWithHistoryAsync(int id, CancellationToken ct);
    Task<Patient?> GetForUpdateAsync(int id, CancellationToken ct);
    Task<bool> ExistsAsync(int id, CancellationToken ct);
    void Add(Patient patient);
}

/// <summary>גישה לנתוני רופאים.</summary>
public interface IDoctorRepository
{
    Task<PagedResult<Doctor>> GetPagedAsync(DoctorQuery query, CancellationToken ct);
    Task<Doctor?> GetByIdAsync(int id, CancellationToken ct);
    Task<Doctor?> GetForUpdateAsync(int id, CancellationToken ct);
    Task<Doctor?> GetForBookingAsync(int id, CancellationToken ct);
    Task<bool> ExistsAsync(int id, CancellationToken ct);
    void Add(Doctor doctor);
}

/// <summary>גישה לנתוני תורים.</summary>
public interface IAppointmentRepository
{
    Task<PagedResult<Appointment>> GetPagedAsync(AppointmentQuery query, CancellationToken ct);
    Task<Appointment?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<Appointment>> GetByPatientIdAsync(int patientId, CancellationToken ct);
    Task<Appointment?> GetForDeleteAsync(int id, CancellationToken ct);
    Task<bool> IsDoctorSlotTakenAsync(int doctorId, DateOnly date, TimeOnly time, CancellationToken ct);
    Task<bool> IsPatientSlotTakenAsync(int patientId, DateOnly date, TimeOnly time, CancellationToken ct);
    Task<List<TimeOnly>> GetDoctorBookedTimesAsync(int doctorId, DateOnly date, CancellationToken ct);
    void Add(Appointment appointment);
    void Remove(Appointment appointment);
}

/// <summary>גישה לתחומי ההתמחות.</summary>
public interface ISpecializationRepository
{
    Task<List<Specialization>> GetAllAsync(CancellationToken ct);
    Task<List<Specialization>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}

/// <summary>גישה למשתמשי המערכת.</summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    void Add(User user);
}

/// <summary>
/// יחידת עבודה: שמירת כל השינויים בטרנזקציה אחת.
/// מתרגמת התנגשות Concurrency לחריגה של Core (ConcurrencyConflictException).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
    void DiscardChanges();
}
