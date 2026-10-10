using DentalClinic.Core.Common;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Enums;
using DentalClinic.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Data.Seed;

/// <summary>מריץ migrations בעליית השרת וזורע נתוני דמו, כך שהמערכת עולה מיד ללא התערבות ידנית.</summary>
public class DbInitializer
{
    public const string AdminEmail = "admin@clinic.local";
    public const string AdminPassword = "Admin123!";
    public const string PatientEmail = "patient@clinic.local";
    public const string PatientPassword = "Patient123!";

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(AppDbContext context, IPasswordHasher passwordHasher, IDateTimeProvider clock, ILogger<DbInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>מחיל migrations (אם הוגדר) וזורע נתונים רק כשהטבלאות ריקות - בטוח להרצה חוזרת.</summary>
    public async Task InitializeAsync(bool applyMigrations, CancellationToken ct)
    {
        if (applyMigrations)
        {
            _logger.LogInformation("Applying database migrations");
            await _context.Database.MigrateAsync(ct);
        }

        if (await _context.Users.AnyAsync(ct))
        {
            _logger.LogDebug("Database already contains data - skipping seed");
            return;
        }

        _logger.LogInformation("Seeding demo data");

        var specializations = await _context.Specializations.ToListAsync(ct);
        Specialization Spec(int id) => specializations.First(s => s.Id == id);

        var doctors = new List<Doctor>
        {
            new()
            {
                Name = "ד\"ר יעקב כהן", Phone = "050-1111111", Gender = Gender.Male,
                DateOfBirth = new DateOnly(1975, 3, 12), EmploymentStartDate = new DateOnly(2010, 9, 1),
                Address = new Address { City = "ירושלים", Street = "יפו", BuildingNumber = "12" },
                Specializations = new List<Specialization> { Spec(1), Spec(3) }
            },
            new()
            {
                Name = "ד\"ר משה לוי", Phone = "052-2222222", Gender = Gender.Male,
                DateOfBirth = new DateOnly(1982, 7, 25), EmploymentStartDate = new DateOnly(2015, 1, 15),
                Address = new Address { City = "בני ברק", Street = "רבי עקיבא", BuildingNumber = "40" },
                Specializations = new List<Specialization> { Spec(2) }
            },
            new()
            {
                Name = "ד\"ר רחל פרידמן", Phone = "054-3333333", Gender = Gender.Female,
                DateOfBirth = new DateOnly(1988, 11, 2), EmploymentStartDate = new DateOnly(2019, 3, 10),
                Address = new Address { City = "פתח תקווה", Street = "רוטשילד", BuildingNumber = "7" },
                Specializations = new List<Specialization> { Spec(1), Spec(4) }
            }
        };

        var patients = new List<Patient>
        {
            new()
            {
                Name = "אברהם ישראלי", Phone = "050-4444444", Gender = Gender.Male, DateOfBirth = new DateOnly(1990, 5, 20),
                Address = new Address { City = "ירושלים", Street = "בר אילן", BuildingNumber = "5" }
            },
            new()
            {
                Name = "שרה גולדברג", Phone = "053-5555555", Gender = Gender.Female, DateOfBirth = new DateOnly(1985, 1, 8),
                Address = new Address { City = "בני ברק", Street = "חזון איש", BuildingNumber = "18" }
            },
            new()
            {
                Name = "יצחק אהרוני", Phone = "058-6666666", Gender = Gender.Male, DateOfBirth = new DateOnly(2001, 9, 30),
                Address = new Address { City = "אלעד", Street = "רבי מאיר", BuildingNumber = "3" }
            }
        };

        _context.Doctors.AddRange(doctors);
        _context.Patients.AddRange(patients);

        var now = _clock.UtcNow;
        _context.Users.AddRange(
            new User { Email = AdminEmail, PasswordHash = _passwordHasher.Hash(AdminPassword), Role = UserRole.Admin, CreatedAtUtc = now },
            new User { Email = PatientEmail, PasswordHash = _passwordHasher.Hash(PatientPassword), Role = UserRole.Patient, Patient = patients[0], CreatedAtUtc = now });

        // תורים לדוגמה: אחד בעבר (היסטוריה) ושניים ביום העבודה הקרוב
        var nextWorkingDay = ClinicHours.NextWorkingDay(_clock.Today);
        var lastMonth = _clock.Today.AddDays(-30);
        while (!ClinicHours.IsWorkingDay(lastMonth))
        {
            lastMonth = lastMonth.AddDays(-1);
        }

        _context.Appointments.AddRange(
            new Appointment { Doctor = doctors[0], Patient = patients[0], Date = lastMonth, Time = new TimeOnly(12, 0), TreatmentType = TreatmentType.Examination, Notes = "בדיקה תקופתית", CreatedAtUtc = now },
            new Appointment { Doctor = doctors[0], Patient = patients[0], Date = nextWorkingDay, Time = new TimeOnly(11, 0), TreatmentType = TreatmentType.Treatment, Notes = "המשך טיפול", CreatedAtUtc = now },
            new Appointment { Doctor = doctors[1], Patient = patients[1], Date = nextWorkingDay, Time = new TimeOnly(14, 30), TreatmentType = TreatmentType.Examination, CreatedAtUtc = now });

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Demo data seeded: {Doctors} doctors, {Patients} patients", doctors.Count, patients.Count);
    }
}
