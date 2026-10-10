namespace DentalClinic.Core.Common;

/// <summary>שמות התפקידים כפי שהם נכתבים ב-JWT ובמאפייני [Authorize].</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Patient = "Patient";
}

/// <summary>שמות claims מותאמים שנכתבים לתוך ה-JWT.</summary>
public static class CustomClaims
{
    public const string PatientId = "patient_id";
}
