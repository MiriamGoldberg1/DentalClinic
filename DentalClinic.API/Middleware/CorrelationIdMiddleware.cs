using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DentalClinic.API.Middleware;

/// <summary>יוצר CorrelationId לכל בקשה, מחזיר אותו ב-header ומכניס אותו ל-scope של הלוג; רושם כל בקשה נכנסת ויוצאת.</summary>
//שימוש ב id כדי שהכתיבה ללוג תהיה מסודרת לפיו
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private static readonly Regex ValidId = new("^[a-zA-Z0-9\\-]{1,64}$", RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // מקבלים מזהה מהלקוח רק אם הוא תקין (מונע הזרקת טקסט לקובץ הלוג)
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = ValidId.IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Request started: {Method} {Path}{Query}",
                context.Request.Method, context.Request.Path, context.Request.QueryString);

            await _next(context);

            _logger.LogInformation("Request finished: {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
