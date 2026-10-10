using System.ComponentModel.DataAnnotations;
using DentalClinic.Core.Enums;

namespace DentalClinic.Core.DTOs.Appointments;

/// <summary>בקשה לקביעת תור. מטופל מחובר לא צריך לשלוח PatientId - הוא נלקח מה-JWT.</summary>
public class AppointmentCreateDto
{
    public DateOnly Date { get; set; }

    public TimeOnly Time { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "יש לבחור רופא")]
    public int DoctorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "מזהה מטופל לא תקין")]
    public int? PatientId { get; set; }

    [EnumDataType(typeof(TreatmentType), ErrorMessage = "סוג טיפול לא תקין (Examination / Treatment)")]
    public TreatmentType TreatmentType { get; set; }

    [StringLength(500, ErrorMessage = "ההערות ארוכות מדי (עד 500 תווים)")]
    public string? Notes { get; set; }
}
