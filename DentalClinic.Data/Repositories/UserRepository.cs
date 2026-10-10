using DentalClinic.Core.Entities;
using DentalClinic.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>משתמש לפי אימייל (האימייל נשמר תמיד באותיות קטנות).</summary>
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users.AnyAsync(u => u.Email == normalized, ct);
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
    }
}
