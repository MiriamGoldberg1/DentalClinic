namespace DentalClinic.API.Models;

/// <summary>מבנה אחיד לכל תשובת שגיאה שהשרת מחזיר (400/401/403/404/409/500).</summary>
public record ErrorResponse(int StatusCode, string Message, string? CorrelationId, IDictionary<string, string[]>? Errors = null);
