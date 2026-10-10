using DentalClinic.Core.Entities;
using DentalClinic.Core.Enums;
using DentalClinic.Core.Exceptions;
using DentalClinic.Data;
using DentalClinic.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DentalClinic.Tests.Concurrency;

/// <summary>
/// ההוכחה שה-Concurrency Token עובד: שני DbContext נפרדים (כמו שתי בקשות HTTP מקבילות) קוראים את אותו רופא,
/// שניהם משנים את היומן שלו, הראשון שומר בהצלחה והשני מקבל DbUpdateConcurrencyException.
/// בסיס הנתונים הוא SQLite בזיכרון - בסיס נתונים אמיתי שמריץ UPDATE ... WHERE Version = @old.
/// </summary>
public sealed class DoctorScheduleConcurrencyTests : IDisposable
{
    private static readonly DateOnly BookingDate = new(2026, 10, 11);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly int _doctorId;
    private readonly int _firstPatientId;
    private readonly int _secondPatientId;

    public DoctorScheduleConcurrencyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();

        var doctor = new Doctor
        {
            Name = "ד\"ר כהן", Phone = "050-1111111", Gender = Gender.Male,
            DateOfBirth = new DateOnly(1975, 1, 1), EmploymentStartDate = new DateOnly(2010, 1, 1),
            Address = new Address { City = "ירושלים", Street = "יפו", BuildingNumber = "1" }
        };
        var first = NewPatient("ראובן");
        var second = NewPatient("שמעון");
        context.AddRange(doctor, first, second);
        context.SaveChanges();

        _doctorId = doctor.Id;
        _firstPatientId = first.Id;
        _secondPatientId = second.Id;
    }

    private static Patient NewPatient(string name) => new()
    {
        Name = name, Phone = "050-2222222", Gender = Gender.Male, DateOfBirth = new DateOnly(1990, 1, 1),
        Address = new Address { City = "בני ברק", Street = "רבי עקיבא", BuildingNumber = "2" }
    };

    private Appointment NewAppointment(int patientId, TimeOnly time) => new()
    {
        DoctorId = _doctorId, PatientId = patientId, Date = BookingDate, Time = time,
        TreatmentType = TreatmentType.Examination, CreatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task TwoContexts_BookSameDoctor_FirstSucceedsSecondThrowsDbUpdateConcurrencyException()
    {
        await using var firstRequest = new AppDbContext(_options);
        await using var secondRequest = new AppDbContext(_options);

        // שתי הבקשות קוראות את הרופא באותו מצב (אותה גרסה)
        var doctorSeenByFirst = await firstRequest.Doctors.SingleAsync(d => d.Id == _doctorId);
        var doctorSeenBySecond = await secondRequest.Doctors.SingleAsync(d => d.Id == _doctorId);
        Assert.Equal(doctorSeenByFirst.Version, doctorSeenBySecond.Version);

        // שתיהן קובעות תור ומחליפות את גרסת היומן
        firstRequest.Appointments.Add(NewAppointment(_firstPatientId, new TimeOnly(11, 0)));
        doctorSeenByFirst.Version = Guid.NewGuid();
        secondRequest.Appointments.Add(NewAppointment(_secondPatientId, new TimeOnly(11, 30)));
        doctorSeenBySecond.Version = Guid.NewGuid();

        // הראשונה שומרת בהצלחה, השנייה נדחית
        await firstRequest.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondRequest.SaveChangesAsync());

        // הטרנזקציה של השנייה בוטלה כולה - נשמר רק התור של הראשונה
        await using var verify = new AppDbContext(_options);
        var saved = await verify.Appointments.ToListAsync();
        Assert.Single(saved);
        Assert.Equal(_firstPatientId, saved[0].PatientId);
    }

    [Fact]
    public async Task UnitOfWork_TranslatesConcurrencyFailureToConcurrencyConflictException()
    {
        await using var firstRequest = new AppDbContext(_options);
        await using var secondRequest = new AppDbContext(_options);
        var firstUnitOfWork = new UnitOfWork(firstRequest, NullLogger<UnitOfWork>.Instance);
        var secondUnitOfWork = new UnitOfWork(secondRequest, NullLogger<UnitOfWork>.Instance);

        var doctorSeenByFirst = await firstRequest.Doctors.SingleAsync(d => d.Id == _doctorId);
        var doctorSeenBySecond = await secondRequest.Doctors.SingleAsync(d => d.Id == _doctorId);

        firstRequest.Appointments.Add(NewAppointment(_firstPatientId, new TimeOnly(15, 0)));
        doctorSeenByFirst.Version = Guid.NewGuid();
        secondRequest.Appointments.Add(NewAppointment(_secondPatientId, new TimeOnly(16, 0)));
        doctorSeenBySecond.Version = Guid.NewGuid();

        await firstUnitOfWork.SaveChangesAsync(CancellationToken.None);

        // שכבת ה-Service מקבלת חריגה של Core (ממופה ל-409), לא חריגה של EF
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => secondUnitOfWork.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SaveChangesOverride_RenewsVersionOnAnyDoctorUpdate()
    {
        await using var firstRequest = new AppDbContext(_options);
        await using var secondRequest = new AppDbContext(_options);

        var doctorSeenByFirst = await firstRequest.Doctors.SingleAsync(d => d.Id == _doctorId);
        var doctorSeenBySecond = await secondRequest.Doctors.SingleAsync(d => d.Id == _doctorId);
        var originalVersion = doctorSeenByFirst.Version;

        // עדכון רגיל של פרטים, בלי לגעת ב-Version - ה-DbContext מחליף את הגרסה בעצמו
        doctorSeenByFirst.Phone = "050-9999999";
        await firstRequest.SaveChangesAsync();
        Assert.NotEqual(originalVersion, doctorSeenByFirst.Version);

        doctorSeenBySecond.Name = "ד\"ר כהן המעודכן";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondRequest.SaveChangesAsync());
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
