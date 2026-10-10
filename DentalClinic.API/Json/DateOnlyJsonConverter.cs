using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DentalClinic.API.Json;

/// <summary>ב-.NET 6 אין תמיכה מובנית ב-DateOnly ב-JSON: הממיר קורא וכותב בפורמט yyyy-MM-dd.</summary>
public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        throw new JsonException($"תאריך לא תקין '{value}'. הפורמט הנדרש: {Format}");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
