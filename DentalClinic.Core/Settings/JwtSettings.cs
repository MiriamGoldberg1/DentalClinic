namespace DentalClinic.Core.Settings;

/// <summary>הגדרות JWT. המפתח (Key) לא נשמר ב-appsettings.json אלא ב-User Secrets / משתני סביבה.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}
