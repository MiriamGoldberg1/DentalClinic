using DentalClinic.Core.Exceptions;
using DentalClinic.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DentalClinic.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// שומר הכול בטרנזקציה אחת (INSERT של התור + UPDATE של גרסת היומן). DbUpdateConcurrencyException מתורגם
    /// ל-ConcurrencyConflictException והפרת אינדקס ייחודי ל-ConflictException - כך ה-Service לא מכיר את EF Core.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning("Optimistic concurrency conflict while saving {Entities}",
                string.Join(", ", ex.Entries.Select(e => e.Metadata.ClrType.Name)));
            throw new ConcurrencyConflictException("הנתונים עודכנו על ידי משתמש אחר באותו רגע. נסה שוב.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            _logger.LogWarning("Unique constraint violation on {Constraint}", pg.ConstraintName);
            throw new ConflictException("הרשומה מתנגשת ברשומה קיימת (למשל תור שכבר נתפס).", ex);
        }
    }

    /// <summary>מנקה את כל הישויות שבמעקב - לפני ניסיון חוזר אחרי התנגשות, כדי לקרוא נתונים טריים.</summary>
    public void DiscardChanges()
    {
        _context.ChangeTracker.Clear();
    }
}
