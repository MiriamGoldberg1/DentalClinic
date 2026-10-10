namespace DentalClinic.Core.Settings;

/// <summary>הגדרות תפעול של המרפאה שנטענות מהקונפיגורציה.</summary>
public class ClinicSettings
{
    public const string SectionName = "Clinic";

    // אזור הזמן של המרפאה - לצורך "היום" ו"עכשיו" (הנתונים עצמם נשמרים ב-UTC)
    public string TimeZoneId { get; set; } = "Asia/Jerusalem";

    // מספר ניסיונות חוזרים כשיש התנגשות על יומן הרופא
    public int MaxBookingAttempts { get; set; } = 3;
}
