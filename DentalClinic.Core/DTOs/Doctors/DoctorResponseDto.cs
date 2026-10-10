using DentalClinic.Core.DTOs.Common;
using DentalClinic.Core.Enums;

namespace DentalClinic.Core.DTOs.Doctors;

/// <summary>פרטי רופא בתשובת ה-API.</summary>
public class DoctorResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = new();
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public DateOnly EmploymentStartDate { get; set; }
    public List<string> Specializations { get; set; } = new();
}

/// <summary>פרטים מקוצרים של רופא, לשימוש בתוך אובייקט תור.</summary>
public class DoctorSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

/// <summary>השעות הפנויות ביומן של רופא בתאריך מסוים.</summary>
public class AvailableSlotsDto
{
    public int DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public List<TimeOnly> Slots { get; set; } = new();
}
