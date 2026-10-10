using DentalClinic.Core.Common;
using DentalClinic.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Data;

/// <summary>ה-DbContext של המערכת: DbSet לכל ישות, והגדרות Fluent API מתיקיית Configurations.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<User> Users => Set<User>();

    /// <summary>טוען את כל מחלקות ה-IEntityTypeConfiguration מהפרויקט הזה.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RenewConcurrencyVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RenewConcurrencyVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// לפני כל שמירה: לכל ישות IConcurrencyVersioned שהשתנתה מוגדרת Version חדשה (אם הקוד לא החליף כבר בעצמו).
    /// EF מוסיף את הגרסה הישנה לתנאי ה-WHERE של ה-UPDATE, ולכן שמירה על סמך נתון מיושן זורקת DbUpdateConcurrencyException.
    /// </summary>
    private void RenewConcurrencyVersions()
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyVersioned>())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var version = entry.Property<Guid>(nameof(IConcurrencyVersioned.Version));
            if (version.CurrentValue == version.OriginalValue)
            {
                version.CurrentValue = Guid.NewGuid();
            }
        }
    }
}
