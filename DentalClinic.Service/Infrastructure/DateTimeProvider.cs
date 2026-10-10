using DentalClinic.Core.Interfaces.Services;
using DentalClinic.Core.Settings;
using Microsoft.Extensions.Options;

namespace DentalClinic.Service.Infrastructure;

/// <summary>שעון המערכת: הנתונים נשמרים ב-UTC, ו"היום" מחושב לפי אזור הזמן של המרפאה (חשוב בענן, ששעונו UTC).</summary>
public class DateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo _clinicTimeZone;

    public DateTimeProvider(IOptions<ClinicSettings> settings)
    {
        _clinicTimeZone = ResolveTimeZone(settings.Value.TimeZoneId);
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(LocalNow);

    public TimeOnly CurrentTime => TimeOnly.FromDateTime(LocalNow);

    private DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _clinicTimeZone);

    /// <summary>מאתר את אזור הזמן גם בשם IANA (לינוקס/ענן) וגם בשם Windows; ברירת מחדל UTC.</summary>
    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        foreach (var candidate in new[] { id, "Asia/Jerusalem", "Israel Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
