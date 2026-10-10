using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Data.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;

    public AppointmentRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>רשימת תורים עם רופא ומטופל (Include), סינון, מיון ודפדוף בבסיס הנתונים.</summary>
    public Task<PagedResult<Appointment>> GetPagedAsync(AppointmentQuery query, CancellationToken ct)
    {
        IQueryable<Appointment> appointments = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor)
            .Include(a => a.Patient);

        if (query.DoctorId.HasValue)
        {
            var doctorId = query.DoctorId.Value;
            appointments = appointments.Where(a => a.DoctorId == doctorId);
        }

        if (query.PatientId.HasValue)
        {
            var patientId = query.PatientId.Value;
            appointments = appointments.Where(a => a.PatientId == patientId);
        }

        if (query.FromDate.HasValue)
        {
            var from = DateOnly.FromDateTime(query.FromDate.Value);
            appointments = appointments.Where(a => a.Date >= from);
        }

        if (query.ToDate.HasValue)
        {
            var to = DateOnly.FromDateTime(query.ToDate.Value);
            appointments = appointments.Where(a => a.Date <= to);
        }

        if (query.TreatmentType.HasValue)
        {
            var treatmentType = query.TreatmentType.Value;
            appointments = appointments.Where(a => a.TreatmentType == treatmentType);
        }

        appointments = (query.SortBy?.ToLowerInvariant(), query.SortDesc) switch
        {
            ("doctor", false) => appointments.OrderBy(a => a.Doctor.Name).ThenBy(a => a.Date).ThenBy(a => a.Time),
            ("doctor", true) => appointments.OrderByDescending(a => a.Doctor.Name).ThenBy(a => a.Date).ThenBy(a => a.Time),
            ("patient", false) => appointments.OrderBy(a => a.Patient.Name).ThenBy(a => a.Date).ThenBy(a => a.Time),
            ("patient", true) => appointments.OrderByDescending(a => a.Patient.Name).ThenBy(a => a.Date).ThenBy(a => a.Time),
            (_, true) => appointments.OrderByDescending(a => a.Date).ThenByDescending(a => a.Time).ThenBy(a => a.Id),
            _ => appointments.OrderBy(a => a.Date).ThenBy(a => a.Time).ThenBy(a => a.Id)
        };

        return appointments.ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    /// <summary>תור לפי מזהה כולל רופא ומטופל - קריאה בלבד.</summary>
    public Task<Appointment?> GetByIdAsync(int id, CancellationToken ct)
    {
        return _context.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    /// <summary>כל התורים של מטופל מסוים, מהחדש לישן.</summary>
    public Task<List<Appointment>> GetByPatientIdAsync(int patientId, CancellationToken ct)
    {
        return _context.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.Time)
            .ToListAsync(ct);
    }

    /// <summary>תור במצב מעקב - לצורך מחיקה.</summary>
    public Task<Appointment?> GetForDeleteAsync(int id, CancellationToken ct)
    {
        return _context.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    /// <summary>האם השעה כבר תפוסה ביומן של הרופא.</summary>
    public Task<bool> IsDoctorSlotTakenAsync(int doctorId, DateOnly date, TimeOnly time, CancellationToken ct)
    {
        return _context.Appointments.AnyAsync(a => a.DoctorId == doctorId && a.Date == date && a.Time == time, ct);
    }

    /// <summary>האם למטופל כבר יש תור (אצל רופא כלשהו) באותו תאריך ושעה.</summary>
    public Task<bool> IsPatientSlotTakenAsync(int patientId, DateOnly date, TimeOnly time, CancellationToken ct)
    {
        return _context.Appointments.AnyAsync(a => a.PatientId == patientId && a.Date == date && a.Time == time, ct);
    }

    /// <summary>השעות התפוסות ביומן הרופא בתאריך מסוים.</summary>
    public Task<List<TimeOnly>> GetDoctorBookedTimesAsync(int doctorId, DateOnly date, CancellationToken ct)
    {
        return _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.Date == date)
            .Select(a => a.Time)
            .ToListAsync(ct);
    }

    public void Add(Appointment appointment)
    {
        _context.Appointments.Add(appointment);
    }

    public void Remove(Appointment appointment)
    {
        _context.Appointments.Remove(appointment);
    }
}
