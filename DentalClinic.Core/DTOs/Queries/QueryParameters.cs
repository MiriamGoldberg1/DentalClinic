using System.ComponentModel.DataAnnotations;
using DentalClinic.Core.Enums;

namespace DentalClinic.Core.DTOs.Queries;

/// <summary>פרמטרים משותפים לדפדוף ולמיון ברשימות.</summary>
public abstract class PagingQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "מספר העמוד חייב להיות 1 ומעלה")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "גודל עמוד חייב להיות בין 1 ל-100")]
    public int PageSize { get; set; } = 10;

    [StringLength(30)]
    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}

/// <summary>סינון רשימת מטופלים: חיפוש לפי שם או טלפון. מיון: name / dateOfBirth / id.</summary>
public class PatientQuery : PagingQuery
{
    [StringLength(100)]
    public string? Search { get; set; }
}

/// <summary>סינון רשימת רופאים: חיפוש לפי שם ולפי תחום התמחות. מיון: name / employmentStartDate / id.</summary>
public class DoctorQuery : PagingQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public int? SpecializationId { get; set; }
}

/// <summary>סינון רשימת תורים: לפי רופא, מטופל, טווח תאריכים וסוג טיפול. מיון: date / doctor / patient.</summary>
public class AppointmentQuery : PagingQuery
{
    public int? DoctorId { get; set; }
    public int? PatientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TreatmentType? TreatmentType { get; set; }
}
