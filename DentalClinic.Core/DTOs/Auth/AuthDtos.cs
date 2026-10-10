using System.ComponentModel.DataAnnotations;
using DentalClinic.Core.DTOs.Patients;

namespace DentalClinic.Core.DTOs.Auth;

/// <summary>הרשמה של מטופל חדש: פרטי המטופל + אימייל וסיסמה.</summary>
public class RegisterDto : PatientRequestDto
{
    [Required(ErrorMessage = "יש להזין אימייל")]
    [EmailAddress(ErrorMessage = "אימייל לא תקין")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "יש להזין סיסמה")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "הסיסמה חייבת להכיל לפחות 8 תווים")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>בקשת התחברות.</summary>
public class LoginDto
{
    [Required(ErrorMessage = "יש להזין אימייל")]
    [EmailAddress(ErrorMessage = "אימייל לא תקין")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "יש להזין סיסמה")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>תשובה להתחברות/הרשמה: ה-JWT ופרטי המשתמש.</summary>
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? PatientId { get; set; }
}
