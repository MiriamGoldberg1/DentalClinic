using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using DentalClinic.API.Middleware;
using DentalClinic.API.Models;

namespace DentalClinic.API.Extensions;


//מחלקת עזר המבטיחה שכל שגיאה שהשרת שולח תראה אותו דבר
public static class ErrorResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <summary>מחזיר את ה-CorrelationId של הבקשה הנוכחית (נוצר ב-CorrelationIdMiddleware).</summary>
    public static string GetCorrelationId(this HttpContext context)
    {
        return context.Items[CorrelationIdMiddleware.ItemKey] as string ?? context.TraceIdentifier;
    }

    /// <summary>כותב לתשובה JSON שגיאה במבנה האחיד של המערכת.</summary>
    public static Task WriteErrorAsync(this HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        var body = new ErrorResponse(statusCode, message, context.GetCorrelationId());
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}