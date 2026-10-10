using DentalClinic.Core.Entities;
using DentalClinic.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Data.Repositories;

public class SpecializationRepository : ISpecializationRepository
{
    private readonly AppDbContext _context;

    public SpecializationRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>כל תחומי ההתמחות, ממוינים לפי שם.</summary>
    public Task<List<Specialization>> GetAllAsync(CancellationToken ct)
    {
        return _context.Specializations.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
    }

    /// <summary>התמחויות לפי רשימת מזהים (במצב מעקב - כדי לקשר אותן לרופא).</summary>
    public Task<List<Specialization>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        return _context.Specializations.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
    }
}
