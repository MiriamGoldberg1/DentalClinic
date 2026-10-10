using AutoMapper;
using DentalClinic.Core.Common;
using DentalClinic.Core.Entities;
using DentalClinic.Core.Exceptions;
using DentalClinic.Core.Interfaces.Repositories;
using DentalClinic.Core.Interfaces.Services;
using DentalClinic.Service.Mapping;
using DentalClinic.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DentalClinic.Tests.Services;

/// <summary>בדיקות יחידה לשכבת ה-Service של מטופלים.</summary>
public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _patients = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    private PatientService CreateService() => new(_patients.Object, _appointments.Object, _unitOfWork.Object, _mapper, _clock.Object,
        NullLogger<PatientService>.Instance);

    [Fact]
    public async Task GetById_PatientAskingForAnotherPatient_ThrowsForbidden()
    {
        var user = new CurrentUser(2, Roles.Patient, 10);

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService().GetByIdAsync(11, user, CancellationToken.None));
    }

    [Fact]
    public async Task GetById_NotExisting_ThrowsNotFound()
    {
        _patients.Setup(p => p.GetByIdWithHistoryAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().GetByIdAsync(42, new CurrentUser(1, Roles.Admin, null), CancellationToken.None));
    }

    [Fact]
    public async Task GetById_ReturnsHistoryOfThatPatientOnly()
    {
        var patient = new Patient
        {
            Id = 10, Name = "אברהם", Phone = "050-1111111", DateOfBirth = new DateOnly(1990, 1, 1),
            Appointments = new List<Appointment>
            {
                new() { Id = 1, PatientId = 10, DoctorId = 5, Date = new DateOnly(2026, 9, 1), Time = new TimeOnly(11, 0), Doctor = new Doctor { Id = 5, Name = "ד\"ר כהן" } },
                new() { Id = 2, PatientId = 10, DoctorId = 5, Date = new DateOnly(2026, 10, 1), Time = new TimeOnly(12, 0), Doctor = new Doctor { Id = 5, Name = "ד\"ר כהן" } }
            }
        };
        _patients.Setup(p => p.GetByIdWithHistoryAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        var result = await CreateService().GetByIdAsync(10, new CurrentUser(2, Roles.Patient, 10), CancellationToken.None);

        Assert.Equal(2, result.History.Count);
        Assert.Equal(2, result.History[0].Id); // מהחדש לישן
        Assert.Equal("ד\"ר כהן", result.History[0].DoctorName);
    }
}
