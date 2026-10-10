using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Data.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _context;

    public DoctorRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>רשימת רופאים עם התמחויות, סינון לפי שם/התמחות, מיון ודפדוף.</summary>
    public Task<PagedResult<Doctor>> GetPagedAsync(DoctorQuery query, CancellationToken ct)
    {
        IQueryable<Doctor> doctors = _context.Doctors
            .AsNoTracking()
            .Include(d => d.Specializations);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            doctors = doctors.Where(d => EF.Functions.ILike(d.Name, pattern));
        }

        if (query.SpecializationId.HasValue)
        {
            var specializationId = query.SpecializationId.Value;
            doctors = doctors.Where(d => d.Specializations.Any(s => s.Id == specializationId));
        }

        doctors = (query.SortBy?.ToLowerInvariant(), query.SortDesc) switch
        {
            ("employmentstartdate", false) => doctors.OrderBy(d => d.EmploymentStartDate).ThenBy(d => d.Id),
            ("employmentstartdate", true) => doctors.OrderByDescending(d => d.EmploymentStartDate).ThenBy(d => d.Id),
            ("id", false) => doctors.OrderBy(d => d.Id),
            ("id", true) => doctors.OrderByDescending(d => d.Id),
            (_, true) => doctors.OrderByDescending(d => d.Name).ThenBy(d => d.Id),
            _ => doctors.OrderBy(d => d.Name).ThenBy(d => d.Id)
        };

        return doctors.ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    /// <summary>רופא לפי מזהה כולל התמחויות - קריאה בלבד.</summary>
    public Task<Doctor?> GetByIdAsync(int id, CancellationToken ct)
    {
        return _context.Doctors
            .AsNoTracking()
            .Include(d => d.Specializations)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    /// <summary>רופא במצב מעקב כולל התמחויות - לצורך עדכון פרטים.</summary>
    public Task<Doctor?> GetForUpdateAsync(int id, CancellationToken ct)
    {
        return _context.Doctors
            .Include(d => d.Specializations)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    /// <summary>
    /// רופא במצב מעקב לצורך קביעת תור. הגרסה (Version) שנקראת כאן היא נקודת הייחוס:
    /// בשמירה EF יוודא שהיא לא השתנתה בינתיים, אחרת תיזרק התנגשות.
    /// </summary>
    public Task<Doctor?> GetForBookingAsync(int id, CancellationToken ct)
    {
        return _context.Doctors.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct)
    {
        return _context.Doctors.AnyAsync(d => d.Id == id, ct);
    }

    public void Add(Doctor doctor)
    {
        _context.Doctors.Add(doctor);
    }
}
