using DentalClinic.Core.DTOs.Doctors;
using DentalClinic.Core.DTOs.Patients;
using DentalClinic.Core.Enums;

namespace DentalClinic.Core.DTOs.Appointments;

/// <summary>פרטי תור מלאים, כולל אובייקט רופא ואובייקט מטופל.</summary>
public class AppointmentResponseDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public DoctorSummaryDto Doctor { get; set; } = new();
    public PatientSummaryDto Patient { get; set; } = new();
    public TreatmentType TreatmentType { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>פרטי תור מקוצרים, להצגה בהיסטוריית המטופל.</summary>
public class AppointmentSummaryDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public TreatmentType TreatmentType { get; set; }
}
