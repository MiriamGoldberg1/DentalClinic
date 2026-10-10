using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Queries;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Data.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;

    public PatientRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>רשימת מטופלים עם חיפוש, מיון ודפדוף - קריאה בלבד (AsNoTracking).</summary>
    public Task<PagedResult<Patient>> GetPagedAsync(PatientQuery query, CancellationToken ct)
    {
        IQueryable<Patient> patients = _context.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            patients = patients.Where(p => EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Phone, pattern));
        }

        patients = (query.SortBy?.ToLowerInvariant(), query.SortDesc) switch
        {
            ("dateofbirth", false) => patients.OrderBy(p => p.DateOfBirth).ThenBy(p => p.Id),
            ("dateofbirth", true) => patients.OrderByDescending(p => p.DateOfBirth).ThenBy(p => p.Id),
            ("id", false) => patients.OrderBy(p => p.Id),
            ("id", true) => patients.OrderByDescending(p => p.Id),
            (_, true) => patients.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            _ => patients.OrderBy(p => p.Name).ThenBy(p => p.Id)
        };

        return patients.ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    /// <summary>מטופל לפי מזהה כולל היסטוריית התורים שלו והרופאים (Include + ThenInclude, בלי N+1).</summary>
    public Task<Patient?> GetByIdWithHistoryAsync(int id, CancellationToken ct)
    {
        return _context.Patients
            .AsNoTracking()
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    /// <summary>מטופל במצב מעקב (Tracking) - לצורך עדכון.</summary>
    public Task<Patient?> GetForUpdateAsync(int id, CancellationToken ct)
    {
        return _context.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct)
    {
        return _context.Patients.AnyAsync(p => p.Id == id, ct);
    }

    public void Add(Patient patient)
    {
        _context.Patients.Add(patient);
    }
}
