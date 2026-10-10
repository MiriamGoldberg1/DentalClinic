using DentalClinic.Core.Enums;

namespace DentalClinic.Core.Entities;

/// <summary>תור: תאריך, שעה, רופא, מטופל וסוג טיפול.</summary>
public class Appointment
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public TreatmentType TreatmentType { get; set; }
    public string? Notes { get; set; }

    // זמן יצירת התור - תמיד ב-UTC
    public DateTime CreatedAtUtc { get; set; }
}
