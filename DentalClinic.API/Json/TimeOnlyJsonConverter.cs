using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DentalClinic.API.Json;

/// <summary>ב-.NET 6 אין תמיכה מובנית ב-TimeOnly ב-JSON: הממיר קורא HH:mm או HH:mm:ss וכותב HH:mm.</summary>
public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] ReadFormats = { "HH:mm", "HH:mm:ss" };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (TimeOnly.TryParseExact(value, ReadFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time;
        }

        throw new JsonException($"שעה לא תקינה '{value}'. הפורמט הנדרש: HH:mm");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("HH:mm", CultureInfo.InvariantCulture));
    }
}
