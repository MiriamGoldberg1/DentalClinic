using DentalClinic.Core.DTOs.Appointments;
using DentalClinic.Core.DTOs.Common;
using DentalClinic.Core.Enums;

namespace DentalClinic.Core.DTOs.Patients;

/// <summary>פרטי מטופל בתשובת ה-API (ברשימה).</summary>
public class PatientResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = new();
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public int Age { get; set; }
}

/// <summary>פרטי מטופל מלאים, כולל History - רשימת התורים שהזמין.</summary>
public class PatientDetailsDto : PatientResponseDto
{
    public List<AppointmentSummaryDto> History { get; set; } = new();
}

/// <summary>פרטים מקוצרים של מטופל, לשימוש בתוך אובייקט תור.</summary>
public class PatientSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
