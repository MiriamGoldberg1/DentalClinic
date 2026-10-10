using AutoMapper;
using DentalClinic.Core.Common;
using DentalClinic.Core.DTOs.Appointments;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Enums;
using DentalClinic.Core.Exceptions;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Core.Interfaces.Services;
using DentalClinic.Core.Settings;
using DentalClinic.Service.Mapping;
using DentalClinic.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DentalClinic.Tests.Services;

/// <summary>בדיקות יחידה לשכבת ה-Service של תורים, עם Repositories מדומים (Moq).</summary>
public class AppointmentServiceTests
{
    private const int DoctorId = 5;
    private const int PatientId = 10;

    // "היום" בבדיקות: יום רביעי 07/10/2026, השעה 09:00
    private static readonly DateOnly Today = new(2026, 10, 7);
    private static readonly DateOnly NextSunday = new(2026, 10, 11);
    private static readonly TimeOnly ValidTime = new(11, 30);

    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IDoctorRepository> _doctors = new();
    private readonly Mock<IPatientRepository> _patients = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    private readonly CurrentUser _admin = new(1, Roles.Admin, null);
    private readonly CurrentUser _patientUser = new(2, Roles.Patient, PatientId);

    public AppointmentServiceTests()
    {
        _clock.Setup(c => c.Today).Returns(Today);
        _clock.Setup(c => c.CurrentTime).Returns(new TimeOnly(9, 0));
        _clock.Setup(c => c.UtcNow).Returns(new DateTime(2026, 10, 7, 6, 0, 0, DateTimeKind.Utc));

        _patients.Setup(p => p.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _doctors.Setup(d => d.GetForBookingAsync(DoctorId, It.IsAny<CancellationToken>())).ReturnsAsync(() => NewDoctor());
        _appointments.Setup(a => a.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Appointment
            {
                Id = id, Date = NextSunday, Time = ValidTime, TreatmentType = TreatmentType.Examination,
                DoctorId = DoctorId, Doctor = NewDoctor(),
                PatientId = PatientId, Patient = new Patient { Id = PatientId, Name = "מטופל בדיקה", Phone = "050-0000000" }
            });
    }

    private static Doctor NewDoctor() => new()
    {
        Id = DoctorId, Name = "ד\"ר בדיקה", Phone = "050-1234567",
        DateOfBirth = new DateOnly(1980, 1, 1), EmploymentStartDate = new DateOnly(2010, 1, 1)
    };

    private AppointmentService CreateService() => new(
        _appointments.Object, _doctors.Object, _patients.Object, _unitOfWork.Object, _mapper, _clock.Object,
        Options.Create(new ClinicSettings { MaxBookingAttempts = 3 }), NullLogger<AppointmentService>.Instance);

    private static AppointmentCreateDto NewRequest(DateOnly? date = null, TimeOnly? time = null, int? patientId = PatientId) => new()
    {
        Date = date ?? NextSunday,
        Time = time ?? ValidTime,
        DoctorId = DoctorId,
        PatientId = patientId,
        TreatmentType = TreatmentType.Examination
    };

    private void SlotIsTaken(bool taken) =>
        _appointments.Setup(a => a.IsDoctorSlotTakenAsync(DoctorId, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(taken);

    // ---------- המשאב המוגבל: המקרה התקין ----------

    [Fact]
    public async Task Create_WhenSlotIsFree_SavesAppointmentAndRenewsDoctorVersion()
    {
        var doctor = NewDoctor();
        var originalVersion = doctor.Version;
        _doctors.Setup(d => d.GetForBookingAsync(DoctorId, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        SlotIsTaken(false);

        var result = await CreateService().CreateAsync(NewRequest(), _admin, CancellationToken.None);

        Assert.Equal(DoctorId, result.Doctor.Id);
        Assert.NotEqual(originalVersion, doctor.Version);
        _appointments.Verify(a => a.Add(It.Is<Appointment>(x => x.DoctorId == DoctorId && x.PatientId == PatientId)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- המשאב המוגבל: המקרה שנדחה ----------

    [Fact]
    public async Task Create_WhenSlotAlreadyTaken_ThrowsConflictAndDoesNotSave()
    {
        SlotIsTaken(true);

        await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(NewRequest(), _admin, CancellationToken.None));

        _appointments.Verify(a => a.Add(It.IsAny<Appointment>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenConcurrentBookingTookTheSlot_RetriesAndThrowsConflict()
    {
        // ניסיון 1: השעה נראתה פנויה, אבל בשמירה התברר שמישהו שינה את היומן בינתיים.
        // ניסיון 2: קוראים שוב - והשעה כבר תפוסה => 409.
        _appointments.SetupSequence(a => a.IsDoctorSlotTakenAsync(DoctorId, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(NewRequest(), _admin, CancellationToken.None));

        Assert.Contains("נתפס", ex.Message);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenConcurrencyConflictButSlotStillFree_RetriesAndSucceeds()
    {
        // מישהו קבע תור אחר אצל אותו רופא באותו רגע - השעה שלנו עדיין פנויה, ולכן הניסיון החוזר מצליח
        SlotIsTaken(false);
        _unitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"))
            .Returns(Task.CompletedTask);

        var result = await CreateService().CreateAsync(NewRequest(), _admin, CancellationToken.None);

        Assert.NotNull(result);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Create_WhenConflictsRepeatEveryAttempt_ThrowsConflictAfterMaxAttempts()
    {
        SlotIsTaken(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("conflict"));

        await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(NewRequest(), _admin, CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    // ---------- כללי שעות הפעילות ----------

    [Theory]
    [InlineData(2026, 10, 9)]   // שישי
    [InlineData(2026, 10, 10)]  // שבת
    public async Task Create_OnFridayOrSaturday_ThrowsBusinessRule(int year, int month, int day)
    {
        var request = NewRequest(date: new DateOnly(year, month, day));

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(request, _admin, CancellationToken.None));
    }

    [Theory]
    [InlineData(10, 30)]  // לפני הפתיחה
    [InlineData(18, 0)]   // בשעת הסגירה
    [InlineData(17, 45)]  // מסתיים אחרי 18:00
    [InlineData(11, 15)]  // לא מיושר לחלון של 30 דקות
    public async Task Create_OutsideOpeningHours_ThrowsBusinessRule(int hour, int minute)
    {
        var request = NewRequest(time: new TimeOnly(hour, minute));

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(request, _admin, CancellationToken.None));
    }

    [Fact]
    public async Task Create_InThePast_ThrowsBusinessRule()
    {
        var request = NewRequest(date: new DateOnly(2026, 10, 4)); // יום ראשון שעבר

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService().CreateAsync(request, _admin, CancellationToken.None));
    }

    // ---------- הרשאות ----------

    [Fact]
    public async Task Create_PatientBookingForAnotherPatient_ThrowsForbidden()
    {
        var request = NewRequest(patientId: 999);

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService().CreateAsync(request, _patientUser, CancellationToken.None));
    }

    [Fact]
    public async Task Create_PatientWithoutPatientId_UsesIdFromToken()
    {
        SlotIsTaken(false);

        await CreateService().CreateAsync(NewRequest(patientId: null), _patientUser, CancellationToken.None);

        _appointments.Verify(a => a.Add(It.Is<Appointment>(x => x.PatientId == PatientId)), Times.Once);
    }

    // ---------- שעות פנויות ----------

    [Fact]
    public async Task GetAvailableSlots_ExcludesBookedTimes()
    {
        _doctors.Setup(d => d.ExistsAsync(DoctorId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _appointments.Setup(a => a.GetDoctorBookedTimesAsync(DoctorId, NextSunday, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeOnly> { new(11, 0), new(12, 30) });

        var result = await CreateService().GetAvailableSlotsAsync(DoctorId, NextSunday, CancellationToken.None);

        Assert.Equal(12, result.Slots.Count); // 14 חלונות (11:00-17:30) פחות 2 תפוסים
        Assert.DoesNotContain(new TimeOnly(11, 0), result.Slots);
        Assert.Contains(new TimeOnly(17, 30), result.Slots);
    }
}
