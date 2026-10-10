using DentalClinic.Core.Enums;

namespace DentalClinic.Core.Entities;

/// <summary>משתמש מערכת להתחברות (JWT). משתמש מסוג מטופל מקושר לרשומת Patient.</summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
