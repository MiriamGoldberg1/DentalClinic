using System.ComponentModel.DataAnnotations;
using DentalClinic.Core.DTOs.Common;
using DentalClinic.Core.Enums;

namespace DentalClinic.Core.DTOs.Doctors;

/// <summary>נתוני רופא בבקשת הוספה או עדכון.</summary>
public class DoctorRequestDto
{
    [Required(ErrorMessage = "יש להזין שם")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "השם חייב להכיל 2-100 תווים")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "יש להזין טלפון")]
    [RegularExpression(@"^0\d{1,2}-?\d{7}$", ErrorMessage = "מספר טלפון לא תקין (לדוגמה 050-1234567)")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "יש להזין כתובת")]
    public AddressDto Address { get; set; } = new();

    public DateOnly DateOfBirth { get; set; }

    [EnumDataType(typeof(Gender), ErrorMessage = "מגדר לא תקין (Male / Female)")]
    public Gender Gender { get; set; }

    public DateOnly EmploymentStartDate { get; set; }

    // מזהי תחומי ההתמחות של הרופא (קשר רבים-לרבים)
    public List<int> SpecializationIds { get; set; } = new();
}
