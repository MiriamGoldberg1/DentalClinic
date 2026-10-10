namespace DentalClinic.Core.Common;

/// <summary>פרטי המשתמש המחובר (מתוך ה-JWT), מועבר לשכבת ה-Service לבדיקות הרשאה.</summary>
public record CurrentUser(int UserId, string Role, int? PatientId)
{
    public bool IsAdmin => Role == Roles.Admin;
}
